using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    internal static class MethodInfoExtensions
    {
        /// <summary>
        /// A method is an SDK method if it has a FunctionNameAttribute AND at least one parameter has an SDK attribute or the method has a NoAutomaticTriggerAttribute.
        /// </summary>
        /// <param name="method">method to check if an SDK method or not.</param>
        /// <returns>true if <paramref name="method"/> is a WebJobs SDK method. False otherwise.</returns>
        public static bool IsWebJobsSdkMethod(this MethodDefinition method)
        {
            return method.HasFunctionNameAttribute() && method.HasValidWebJobSdkTriggerAttribute();
        }

        public static bool HasValidWebJobSdkTriggerAttribute(this MethodDefinition method)
        {
            var hasNoAutomaticTrigger = method.HasNoAutomaticTriggerAttribute();
            var hasTrigger = method.HasTriggerAttribute();
            return (hasNoAutomaticTrigger || hasTrigger) && !(hasNoAutomaticTrigger && hasTrigger);
        }

        public static bool HasFunctionNameAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(d => d.AttributeType.FullName == "Microsoft.Azure.WebJobs.FunctionNameAttribute") != null;
        }

        public static bool HasNoAutomaticTriggerAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.NoAutomaticTriggerAttribute") != null;
        }

        public static bool HasTriggerAttribute(this MethodDefinition method)
        {
            return method.Parameters.Any(p => p.IsWebJobSdkTriggerParameter());
        }

        public static JObject ManualTriggerBinding(this MethodDefinition method)
        {
            var binding = new JObject { ["type"] = "manualTrigger", ["direction"] = "in" };
            var stringParameter = method.Parameters.FirstOrDefault(p => p.ParameterType.FullName == typeof(string).FullName);
            if (stringParameter != null)
            {
                binding["name"] = stringParameter.Name;
            }
            return binding;
        }

        /// <summary>
        /// Converts a <see cref="MethodInfo"/> to a <see cref="FunctionJsonSchema"/>
        /// </summary>
        /// <param name="method">method to convert to a <see cref="FunctionJsonSchema"/> object. The method has to be <see cref="IsWebJobsSdkMethod(MethodInfo)"/> </param>
        /// <param name="assemblyPath">This will be the value of <see cref="FunctionJsonSchema.ScriptFile"/> on the returned value.</param>
        /// <returns><see cref="FunctionJsonSchema"/> object that represents the passed in <paramref name="method"/>.</returns>
        public static FunctionJsonSchema ToFunctionJson(this MethodDefinition method, string assemblyPath)
        {
            // For every SDK parameter, convert it to a FunctionJson bindings.
            // Every parameter can potentially contain more than 1 attribute that will be converted into a binding object.
            var bindingsFromParameters = method.HasNoAutomaticTriggerAttribute() ? new[] { method.ManualTriggerBinding() } : method.Parameters
                                            .Select(p => p.ToFunctionJsonBindings())
                                            .SelectMany(i => i)
                                            .ToArray();

            // Get binding if a return attribute is used.
            // Ex:  [return: Queue("myqueue-items-a", Connection = "MyStorageConnStr")]
            var returnBindings = GetOutputBindingsFromReturnAttribute(method);
            var allBindings = bindingsFromParameters.Concat(returnBindings);

            return new FunctionJsonSchema
            {
                Bindings = allBindings,
                // Entry point is the fully qualified name of the function
                EntryPoint = $"{method.DeclaringType.FullName}.{method.Name}",
                ScriptFile = assemblyPath,
                // A method is disabled is any of it's parameters have [Disabled] attribute
                // or if the method itself or class have the [Disabled] attribute.
                Disabled = method.GetDisabled()
            };
        }

        /// <summary>
        /// Gets bindings from return expression used with a binding expression.
        /// Ex:
        ///     [FunctionName("HttpTriggerWriteToQueue1")]
        ///     [return: Queue("myqueue-items-a", Connection = "MyStorageConnStra")]
        ///     public static string Run([HttpTrigger] HttpRequestMessage request) => "foo";
        /// </summary>
        private static JObject[] GetOutputBindingsFromReturnAttribute(MethodDefinition method)
        {
            if (method.MethodReturnType == null)
            {
                return Array.Empty<JObject>();
            }

            var outputBindings = new List<JObject>();
            foreach (var attribute in method.MethodReturnType.CustomAttributes)
            {
                if (!attribute.AttributeType.FullName.StartsWith("Microsoft.Azure.WebJobs"))
                {
                    continue;
                }

                var bindingJObject = attribute.ToReflection().ToJObject();
                
                bindingJObject["name"] = "$return";
                bindingJObject["Direction"] = "out";

                outputBindings.Add(bindingJObject);
            }

            return outputBindings.ToArray();
        }

        /// <summary>
        /// Gets a function name from a <paramref name="method"/>
        /// </summary>
        /// <param name="method">method has to be a WebJobs SDK method. <see cref="IsWebJobsSdkMethod(MethodInfo)"/></param>
        /// <returns>Function name.</returns>
        public static string GetSdkFunctionName(this MethodDefinition method)
        {
            if (!method.IsWebJobsSdkMethod())
            {
                throw new ArgumentException($"{nameof(method)} has to be a WebJob SDK function");
            }

            string functionName = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "FunctionNameAttribute")?.ConstructorArguments[0].Value.ToString();
            if (functionName != null)
            {
                return functionName;
            }
            else
            {
                throw new InvalidOperationException("Missing FunctionNameAttribute");
            }
        }

        /// <summary>
        /// A method is disabled is any of it's parameters have [Disabled] attribute
        /// or if the method itself or class have the [Disabled] attribute. The overloads
        /// are stringified so that the ScriptHost will do its job.
        /// </summary>
        /// <param name="method"></param>
        /// <returns>a boolean true or false if the outcome is fixed, a string if the ScriptHost should interpret it</returns>
        public static object GetDisabled(this MethodDefinition method)
        {
            var customAttribute = method.Parameters.Select(p => p.GetDisabledAttribute()).Where(a => a != null).FirstOrDefault() ??
                method.GetDisabledAttribute() ??
                method.DeclaringType.GetDisabledAttribute();

            if (customAttribute != null)
            {
                var attribute = customAttribute.ToReflection();

                // With a SettingName defined, just put that as string. The ScriptHost will evaluate it.
                var settingName = attribute.GetValue<string>("SettingName");
                if (!string.IsNullOrEmpty(settingName))
                {
                    return settingName;
                }

                var providerType = attribute.GetValue<Type>("ProviderType");
                if (providerType != null)
                {
                    return providerType;
                }

                // With neither settingName or providerType, no arguments were given and it should always be true

                return true;
            }

            // No attribute means not disabled
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static CustomAttribute GetDisabledAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }

        /// <summary>
        /// A method has an unsupported attributes if it has any of the following:
        ///     1) [Disabled("%settingName%")]
        ///     2) [Disabled(typeof(TypeName))]
        /// However this [Disabled("settingName")] is valid.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool HasUnsuportedAttributes(this MethodDefinition method, out string error)
        {
            error = string.Empty;
            var disabled = method.GetDisabled();
            if (disabled is string disabledStr &&
                disabledStr.StartsWith("%") &&
                disabledStr.EndsWith("%"))
            {
                error = "'%' expressions are not supported for 'Disable'. Use 'Disable(\"settingName\") instead of 'Disable(\"%settingName%\")'";
                return true;
            }
            else if (disabled is Type)
            {
                error = "the constructor 'DisableAttribute(Type)' is not supported.";
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
