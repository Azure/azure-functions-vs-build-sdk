using MakeFunctionJson;

namespace Microsoft.NET.Sdk.Functions.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Logger.LogError("USAGE: <assemblyPath> <outputPath>");
            }
            else
            {
                var assemblyPath = args[0];
                var outputPath = args[1];
                var converter = new FunctionJsonConverter(assemblyPath, outputPath);
                if (!converter.TryRun())
                {
                    Logger.LogError("Error generating functions metadata");
                }
            }
        }
    }
}