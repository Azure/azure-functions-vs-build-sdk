using Microsoft.Build.Utilities;

namespace MakeFunctionJson
{
    public static class FunctionJsonConvert
    {
        public static bool TryConvert(string assemblyPath, string outputPath, TaskLoggingHelper log = null)
        {
            var converter = new FunctionJsonConverter(assemblyPath, outputPath, log);
            return converter.TryRun();
        }
    }
}
