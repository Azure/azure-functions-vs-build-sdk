using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
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
        public static bool IsWebJobSdkTriggerParameter(this ParameterDefinition parameterInfo)
        {
            return parameterInfo
               .CustomAttributes
               .Any(a => a.IsWebJobsAttribute() && a.ToAttributeFriendlyName().IndexOf("Trigger") > -1);
        }

        /// <summary>
        /// Every parameter can be 1 to N bindings.
        /// </summary>
        /// <param name="parameterInfo">Has to be a WebJobSdkParameter <see cref="IsWebJobsSdkParameter(ParameterInfo)"/></param>
        /// <returns></returns>
        public static IEnumerable<JObject> ToFunctionJsonBindings(this ParameterDefinition parameterInfo)
        {
            return parameterInfo
                .CustomAttributes
                .Where(a => a.IsWebJobsAttribute()) // this has to return at least 1.
                .Select(a => TypeUtility.GetResolvedAttribute(parameterInfo, a)) // For IConnectionProvider logic.
                .Select(a => a.ToJObject()) // Convert the Attribute into a JObject.
                .Select(obj =>
                {
                    // Add a name property on the JObject that refers to the parameter name.
                    obj["name"] = parameterInfo.Name;
                    return obj;
                })
                .ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        public static CustomAttribute GetDisabledAttribute(this ParameterDefinition parameterInfo)
        {
            return parameterInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }
    }
}
