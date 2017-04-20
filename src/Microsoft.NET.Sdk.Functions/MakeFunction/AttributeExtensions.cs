using System;
using System.Linq;
using Microsoft.NET.Sdk.Functions.MakeFunction;

namespace MakeFunctionJson
{
    internal static class AttributeExtensions
    {
        /// <summary>
        /// {Name}Attribute -> name
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static string ToAttributeFriendlyName(this Attribute attribute)
        {
            const string suffix = nameof(Attribute);
            var name = attribute.GetType().Name;
            name = name.Substring(0, name.Length - suffix.Length);
            return name.ToLowerFirstCharacter();
        }
    }
}
