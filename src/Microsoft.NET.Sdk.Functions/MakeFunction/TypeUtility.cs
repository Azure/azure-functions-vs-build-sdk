using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MakeFunctionJson
{
    /// <summary>
    /// Code is __mostly__ a copy of https://github.com/Azure/azure-webjobs-sdk/blob/ab0ad6449460b70534ab0472ee4bbb92be8157fc/src/Microsoft.Azure.WebJobs.Host/TypeUtility.cs
    /// </summary>
    internal static class TypeUtility
    {
        internal static string GetFriendlyName(Type type)
        {
            if (TypeUtility.IsNullable(type))
            {
                return string.Format(CultureInfo.InvariantCulture, "Nullable<{0}>", type.GetGenericArguments()[0].Name);
            }
            else
            {
                return type.Name;
            }
        }

        internal static bool IsNullable(Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static bool IsJObject(Type type)
        {
            return type == typeof(JObject);
        }

        // Task<T> --> T
        // Task --> void
        // T --> T
        internal static Type UnwrapTaskType(Type type)
        {
            if (type == typeof(Task))
            {
                return typeof(void);
            }
            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        /// <summary>
        /// Walk from the parameter up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        internal static T GetHierarchicalAttributeOrNull<T>(ParameterInfo parameter) where T : Attribute
        {
            return (T)GetHierarchicalAttributeOrNull(parameter, typeof(T));
        }

        /// <summary>
        /// Walk from the parameter up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="attributeType">The attribute type to look for.</param>
        internal static Attribute GetHierarchicalAttributeOrNull(ParameterInfo parameter, Type attributeType)
        {
            if (parameter == null)
            {
                return null;
            }

            var attribute = parameter.GetCustomAttribute(attributeType);
            if (attribute != null)
            {
                return attribute;
            }

            var method = parameter.Member as MethodInfo;
            if (method == null)
            {
                return null;
            }
            return GetHierarchicalAttributeOrNull(method, attributeType);
        }

        /// <summary>
        /// Walk from the method up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="method">The method to check.</param>
        internal static T GetHierarchicalAttributeOrNull<T>(MethodInfo method) where T : Attribute
        {
            return (T)GetHierarchicalAttributeOrNull(method, typeof(T));
        }

        /// <summary>
        /// Walk from the method up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <param name="type">The attribute type to look for.</param>
        internal static Attribute GetHierarchicalAttributeOrNull(MethodInfo method, Type type)
        {
            var attribute = method.GetCustomAttribute(type);
            if (attribute != null)
            {
                return attribute;
            }

            attribute = method.DeclaringType.GetTypeInfo().GetCustomAttribute(type);
            if (attribute != null)
            {
                return attribute;
            }

            return null;
        }

        internal static Attribute GetResolvedAttribute(ParameterInfo parameter, Attribute attribute)
        {
            if (attribute != null &&
                attribute.GetType().GetTypeInfo().IsImplementing("IConnectionProvider") &&
                string.IsNullOrEmpty(attribute.GetValue<string>("Connection")))
            {
                // if the attribute doesn't specify an explicit connection, walk up
                // the hierarchy looking for an override specified via attribute
                var connectionProviderAttribute = attribute
                    .GetType()
                    .GetTypeInfo()
                    .GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType().Name == "ConnectionProviderAttribute");

                if (connectionProviderAttribute?.GetValue<Type>("ProviderType") != null)
                {
                    var connectionOverrideProvider = GetHierarchicalAttributeOrNull(parameter, connectionProviderAttribute.GetValue<Type>("ProviderType"));
                    if (connectionOverrideProvider != null &&
                        connectionOverrideProvider.GetType().GetTypeInfo().IsImplementing("IConnectionProvider"))
                    {
                        var iConnectionProvider = connectionOverrideProvider.GetType().GetTypeInfo().GetInterface("IConnectionProvider");
                        var propertyInfo = iConnectionProvider.GetProperty("Connection");
                        var connectionValue = (string) propertyInfo.GetValue(attribute);
                        connectionValue = connectionValue 
                            ?? connectionOverrideProvider.GetValue<string>("Connection")
                            ?? connectionOverrideProvider.GetValue<string>("Account");
                        if (!string.IsNullOrEmpty(connectionValue))
                        {
                            attribute.SetValue("Connection", connectionValue);
                        }
                    }
                }
            }

            return attribute;
        }

        public static bool IsAsync(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var stateMachineAttribute = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (stateMachineAttribute != null)
            {
                var stateMachineType = stateMachineAttribute.StateMachineType;
                if (stateMachineType != null)
                {
                    return stateMachineType.GetTypeInfo().GetCustomAttribute<CompilerGeneratedAttribute>() != null;
                }
            }
            return false;
        }

        public static bool IsAsyncVoid(MethodInfo methodInfo)
        {
            return IsAsync(methodInfo) && (methodInfo.ReturnType == typeof(void));
        }
    }
}
