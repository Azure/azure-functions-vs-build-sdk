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
            return method.GetParameters().Any(p => p.IsWebJobSdkTriggerParameter());
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
            return new FunctionJsonSchema
            {
                // For every SDK parameter, convert it to a FunctionJson bindings.
                // Every parameter can potentially contain more than 1 attribute that will be converted into a binding object.
                Bindings = method.GetParameters()
                    .Where(p => p.IsWebJobSdkTriggerParameter())
                    .Select(p => p.ToFunctionJsonBindings())
                    .SelectMany(i => i)
                    .ToArray(),
                // Entry point is the fully qualified name of the function
                EntryPoint = $"{method.DeclaringType.FullName}.{method.Name}",
                // scriptFile == assemblyPath.
                ScriptFile = assemblyPath
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
    }
}
