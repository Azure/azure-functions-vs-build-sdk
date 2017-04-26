using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    internal static class ParameterInfoExtensions
    {
        /// <summary>
        /// A parameter is an SDK parameter if it has at lease 1 SDK attribute.
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        public static bool IsWebJobsSdkParameter(this ParameterInfo parameterInfo)
        {
            return parameterInfo
                .GetCustomAttributes()
                .Any(a => a.IsWebJobsAttribute());
        }

        /// <summary>
        /// Every parameter can be 1 to N bindings.
        /// </summary>
        /// <param name="parameterInfo">Has to be a WebJobSdkParameter <see cref="IsWebJobsSdkParameter(ParameterInfo)"/></param>
        /// <returns></returns>
        public static IEnumerable<JObject> ToFunctionJsonBindings(this ParameterInfo parameterInfo)
        {

            var bindings = parameterInfo
                .GetCustomAttributes()
                .Where(a => a.IsWebJobsAttribute()) // this has to return at least 1.
                .Select(a => TypeUtility.GetResolvedAttribute(parameterInfo, a))
                .Select(a => a.ToJObject()) // Convert the Attribute into a JObject.
                .Select(obj =>
                {
                    // Add a name property on the JObject that refers to the parameter name.
                    obj["name"] = parameterInfo.Name;
                    return obj;
                })
                .ToList();

            // If there is an httpTrigger, add a $return for http response.
            if (bindings.Any(b => b["type"]?.ToString() == "httpTrigger"))
            {
                return bindings.Concat(new[] { JObject.FromObject(new { name = "$return", type = "http", direction = "out" }) });
            }
            else
            {
                return bindings;
            }
        }
    }
}
