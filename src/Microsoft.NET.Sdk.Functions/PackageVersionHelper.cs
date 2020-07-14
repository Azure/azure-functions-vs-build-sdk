using System.Reflection;

namespace Microsoft.NET.Sdk.Functions
{
    public static class PackageVersionHelper
    {
        public static string Name => typeof(PackageVersionHelper).GetTypeInfo().Assembly.GetName().Name;

        public static string Version => typeof(PackageVersionHelper).GetTypeInfo().Assembly.GetName().Version.ToString(3);
    }
}
