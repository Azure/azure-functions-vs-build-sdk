using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;

namespace MakeFunctionJson
{
    /// <summary>
    /// Code is __mostly__ a copy of https://github.com/Azure/azure-webjobs-sdk/blob/ab0ad6449460b70534ab0472ee4bbb92be8157fc/src/Microsoft.Azure.WebJobs.Host/TypeUtility.cs
    /// </summary>
    internal static class TypeUtility
    {
        /// <summary>
        /// Walk from the parameter up to the containing type, looking for an instance
        /// of the specified attribute type, returning it if found.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="attributeType">The attribute type to look for.</param>
        private static CustomAttribute GetHierarchicalAttributeOrNull(ParameterDefinition parameter, Type attributeType)
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

            var method = parameter.Method as MethodDefinition;
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
        /// <param name="type">The attribute type to look for.</param>
        private static CustomAttribute GetHierarchicalAttributeOrNull(Mono.Cecil.MethodDefinition method, Type type)
        {
            var attribute = method.GetCustomAttribute(type);
            if (attribute != null)
            {
                return attribute;
            }

            attribute = method.DeclaringType.GetCustomAttribute(type);
            if (attribute != null)
            {
                return attribute;
            }

            return null;
        }

        internal static Attribute GetResolvedAttribute(ParameterDefinition parameter, CustomAttribute customAttribute)
        {
            Attribute attribute = customAttribute.ToReflection();

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
                    var connectionOverrideProvider = GetHierarchicalAttributeOrNull(parameter, connectionProviderAttribute.GetValue<Type>("ProviderType"))?.ToReflection();
                    if (connectionOverrideProvider != null &&
                        connectionOverrideProvider.GetType().GetTypeInfo().IsImplementing("IConnectionProvider"))
                    {
                        var iConnectionProvider = connectionOverrideProvider.GetType().GetTypeInfo().GetInterface("IConnectionProvider");
                        var propertyInfo = iConnectionProvider.GetProperty("Connection");
                        var connectionValue = (string)propertyInfo.GetValue(attribute);
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

        public static Attribute ToReflection(this CustomAttribute customAttribute)
        {
            var attributeType = customAttribute.AttributeType.ToReflectionType();

            Type[] constructorParams = customAttribute.Constructor.Parameters
                 .Select(p => p.ParameterType.ToReflectionType())
                 .ToArray();

            Attribute attribute = attributeType.GetConstructor(constructorParams)
                .Invoke(customAttribute.ConstructorArguments.Select(p => NormalizeArg(p)).ToArray()) as Attribute;

            foreach (var namedArgument in customAttribute.Properties)
            {
                attributeType.GetProperty(namedArgument.Name)?.SetValue(attribute, namedArgument.Argument.Value);
                attributeType.GetField(namedArgument.Name)?.SetValue(attribute, namedArgument.Argument.Value);
            }

            return attribute;
        }

        public static Type ToReflectionType(this TypeReference typeDef)
        {
            Type t = Type.GetType(typeDef.GetReflectionFullName());

            if (t == null)
            {
                Assembly a = AssemblyLoadContext.Default.LoadFromAssemblyPath(typeDef.Resolve().Module.FileName);
                t = a.GetType(typeDef.GetReflectionFullName());
            }

            return t;
        }

        private static object NormalizeArg(CustomAttributeArgument arg)
        {
            if (arg.Type.IsArray)
            {
                var arguments = arg.Value as CustomAttributeArgument[];
                Type arrayType = arg.Type.GetElementType().ToReflectionType();
                var array = Array.CreateInstance(arrayType, arguments.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(arguments[i].Value, i);
                }
                return array;
            }

            if (arg.Value is TypeDefinition typeDef)
            {
                return typeDef.ToReflectionType();
            }

            return arg.Value;
        }

        public static CustomAttribute GetCustomAttribute(this Mono.Cecil.ICustomAttributeProvider provider, Type parameterType)
        {
            return provider.CustomAttributes.SingleOrDefault(p => p.AttributeType.FullName == parameterType.FullName);
        }

        public static string GetReflectionFullName(this TypeReference typeRef)
        {
            return typeRef.FullName.Replace("/", "+");
        }
    }
}
