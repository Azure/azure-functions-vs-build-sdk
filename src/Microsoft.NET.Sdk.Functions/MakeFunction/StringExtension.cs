using System;
using System.Linq;

namespace Microsoft.NET.Sdk.Functions.MakeFunction
{
    public static class StringExtension
    {
        public static string ToLowerFirstCharacter(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return Char.ToLowerInvariant(str.First()) + str.Substring(1);
            }
            else
            {
                return str;
            }
        }
    }
}
