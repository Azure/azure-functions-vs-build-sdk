using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.NET.Sdk.Functions.Generator
{
    public static class CustomAttributeDataExtensions
    {
        public static Attribute ConvertToAttribute(this CustomAttributeData data)
        {
            var attribute = data.Constructor.Invoke(data.ConstructorArguments.Select(arg => arg.Value).ToArray()) as Attribute;

            foreach (var namedArgument in data.NamedArguments)
            {
                (namedArgument.MemberInfo as PropertyInfo)?.SetValue(attribute, namedArgument.TypedValue.Value, null);
                (namedArgument.MemberInfo as FieldInfo)?.SetValue(attribute, namedArgument.TypedValue.Value);
            }

            return attribute;
        }
    }
}
