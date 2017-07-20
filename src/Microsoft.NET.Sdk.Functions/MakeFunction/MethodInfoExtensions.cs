using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    internal static class MethodInfoExtensions
    {
        /// <summary>
        /// A method is an SDK method if it has a FunctionNameAttribute AND at least one parameter has an SDK attribute.
        /// </summary>
        /// <param name="method">method to check if an SDK method or not.</param>
        /// <returns>true if <paramref name="method"/> is a WebJobs SDK method. False otherwise.</returns>
        public static bool IsWebJobsSdkMethod(this MethodInfo method)
        {
            return method.HasFunctionNameAttribute() && method.HasWebJobSdkAttribute();
        }

        public static bool HasWebJobSdkAttribute(this MethodInfo method)
        {
            return method.GetParameters().Any(p => p.IsWebJobsSdkParameter());
        }

        public static bool HasFunctionNameAttribute(this MethodInfo method)
        {
            return method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "FunctionNameAttribute") != null;
        }

        /// <summary>
        /// Converts a <see cref="MethodInfo"/> to a <see cref="FunctionJsonSchema"/>
        /// </summary>
        /// <param name="method">method to convert to a <see cref="FunctionJsonSchema"/> object. The method has to be <see cref="IsWebJobsSdkMethod(MethodInfo)"/> </param>
        /// <param name="assemblyPath">This will be the value of <see cref="FunctionJsonSchema.ScriptFile"/> on the returned value.</param>
        /// <returns><see cref="FunctionJsonSchema"/> object that represents the passed in <paramref name="method"/>.</returns>
        public static FunctionJsonSchema ToFunctionJson(this MethodInfo method, string assemblyPath)
        {
            var bindings = method.GetParameters()
                .Where(p => p.IsWebJobsSdkParameter())
                .Select(p => p.ToFunctionJsonBindings())
                .SelectMany(i => i)
                .ToArray();

            var returnOutputBindings = method
                .ReturnTypeCustomAttributes
                .GetCustomAttributes(false)
                .Cast<Attribute>()
                .Where(a => a.IsWebJobsAttribute())
                .Select(a => a.ToJObject())
                .Select(a =>
                {
                    a["name"] = "$return";
                    return a;
                })
                .ToArray();

            // If there is an httpTrigger and no $return binding, always add an http $return.
            if (!returnOutputBindings.Any() && bindings.Any(b => b["type"]?.ToString() == "httpTrigger"))
            {
                returnOutputBindings = new[] { JObject.FromObject(new { name = "$return", type = "http", direction = "out" }) };
            }

            return new FunctionJsonSchema
            {
                // For every SDK parameter, convert it to a FunctionJson bindings.
                // Every parameter can potentially contain more than 1 attribute that will be converted into a binding object.
                Bindings = bindings.Concat(returnOutputBindings),
                // Entry point is the fully qualified name of the function
                EntryPoint = $"{method.DeclaringType.FullName}.{method.Name}",
                // scriptFile == assemblyPath.
                ScriptFile = assemblyPath,
                // A method is disabled is any of it's parameters have [Disabled] attribute
                // or if the method itself or class have the [Disabled] attribute.
                Disabled = method.GetDisabled()
            };
        }

        /// <summary>
        /// Gets a function name from a <paramref name="method"/>
        /// </summary>
        /// <param name="method">method has to be a WebJobs SDK method. <see cref="IsWebJobsSdkMethod(MethodInfo)"/></param>
        /// <returns>Function name.</returns>
        public static string GetSdkFunctionName(this MethodInfo method)
        {
            if (!method.IsWebJobsSdkMethod())
            {
                throw new ArgumentException($"{nameof(method)} has to be a WebJob SDK function");
            }

            var functionNameAttribute = method.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "FunctionNameAttribute");
            if (functionNameAttribute != null)
            {
                return functionNameAttribute.GetType().GetProperty("Name").GetValue(functionNameAttribute).ToString();
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
        public static object GetDisabled(this MethodInfo method)
        {
            var attribute = method.GetParameters().Select(p => p.GetDisabledAttribute()).Where(a => a != null).FirstOrDefault() ??
                method.GetDisabledAttribute() ??
                method.DeclaringType.GetTypeInfo().GetDisabledAttribute();
            if (attribute != null)
            {
                // With a SettingName defined, just put that as string. The ScriptHost will evaluate it.
                var settingName = attribute.GetValue<string>("SettingName");
                if (!string.IsNullOrEmpty(settingName))
                {
                    return settingName;
                }

                // Although Microsoft.Azure.WebJobs.Script.ScriptHost.IsDisabled(..) implementation suggests this is not supported (yet), we'll write the full
                // type name in which the IsDisabled method would be searched if it were implemented. Assuming this is how it would be implemented.
                // This assumption is based on the discussion in https://github.com/Azure/azure-webjobs-sdk/issues/578
                var providerType = attribute.GetValue<Type>("ProviderType");
                if (providerType != null)
                {
                    // Test if this type actually has an IsDisabled method matching the signature.
                    var isDisabledMethod = providerType.GetMethod("IsDisabled", new[] { typeof(MethodInfo) });
                    if (isDisabledMethod == null || isDisabledMethod.ReturnType != typeof(bool))
                    {
                        // The developer defined a type that has no IsDisabled method.
                        throw new MissingMethodException($"The IsDisabled method does not exist in given type {providerType.FullName} or does not have the correct signature");
                    }
                    return providerType.FullName;
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
        public static Attribute GetDisabledAttribute(this MethodInfo method)
        {
            return method.GetCustomAttributes().FirstOrDefault(a => a.GetType().FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }
    }
}
