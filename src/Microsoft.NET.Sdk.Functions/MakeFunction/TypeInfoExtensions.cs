using System;
using System.Linq;
using System.Reflection;

namespace MakeFunctionJson
{
    internal static class TypeInfoExtensions
    {
        public static bool IsImplementing(this TypeInfo typeInfo, string interfaceName)
        {
            return typeInfo.ImplementedInterfaces.Any(i => i.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasDisabledAttribute(this TypeInfo type)
        {
            return type.GetCustomAttributes().Any(a => a.GetType().FullName == "Microsoft.Azure.WebJobs.DisableAttribute");
        }
    }
}
