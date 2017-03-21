using System;
using System.Linq;
using System.Reflection;

namespace MakeFunctionJson
{
    internal static class MethodInfoExtensions
    {
        /// <summary>
        /// A method is an SDK method if any of its parameters has an SDK attribute
        /// </summary>
        /// <param name="method">method to check if an SDK method or not.</param>
        /// <returns>true if <paramref name="method"/> is a WebJobs SDK method. False otherwise.</returns>
        public static bool IsWebJobsSdkMethod(this MethodInfo method)
        {
            // TODO: This will have to add && method has FunctionNameAttribute.
            return method.GetParameters().Any(p => p.IsWebJobsSdkParameter());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method">method to convert to a <see cref="FunctionJsonSchema"/> object.</param>
        /// <param name="assemblyPath">This will be the value of <see cref="FunctionJsonSchema.ScriptFile"/> on the returned value.</param>
        /// <returns></returns>
        public static FunctionJsonSchema ToFunctionJson(this MethodInfo method, string assemblyPath)
        {
            return new FunctionJsonSchema
            {
                // For SDK parameter, convert it to a FunctionJson bindings.
                // Every parameter can potentially contain more than 1 attribute that will be converted into a binding object.
                Bindings = method.GetParameters().Where(p => p.IsWebJobsSdkParameter()).Select(p => p.ToFunctionJsonBindings()).SelectMany(i => i),
                // Entry point is the fully qualified name of the function
                EntryPoint = $"{method.DeclaringType.FullName}.{method.Name}",
                // scriptFile == assemblyPath.
                ScriptFile = assemblyPath
            };
        }

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
                var name = method.DeclaringType.Name;
                const string suffix = "Function";
                if (!name.EndsWith(suffix))
                {
                    throw new InvalidOperationException("By convention, class name must end with '" + suffix + "'");
                }
                return name.Substring(0, name.Length - suffix.Length);
            }
        }
    }
}
