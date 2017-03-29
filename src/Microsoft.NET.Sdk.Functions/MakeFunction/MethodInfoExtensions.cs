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
                Bindings = method.GetParameters().Where(p => p.IsWebJobsSdkParameter()).Select(p => p.ToFunctionJsonBindings()).SelectMany(i => i),
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
                // If there is a [FunctionNameAttribute] on the function, use that to get the name of the function.
                return functionNameAttribute.GetType().GetProperty("Name").GetValue(functionNameAttribute).ToString();
            }
            else
            {
                // Otherwise, use a naming convention that comes from the ClassName
                // class {FunctionName}Function { }
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
