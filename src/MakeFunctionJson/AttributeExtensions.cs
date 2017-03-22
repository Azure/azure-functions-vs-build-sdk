using System;
using System.Linq;

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
            return Char.ToLowerInvariant(name.First()) + name.Substring(1);
        }
    }
}
