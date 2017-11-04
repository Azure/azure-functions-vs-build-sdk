using MakeFunctionJson;

namespace Microsoft.NET.Sdk.Functions.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var logger = new ConsoleLogger();
            if (args.Length != 2)
            {
                logger.LogError("USAGE: <assemblyPath> <outputPath>");
            }
            else
            {
                var assemblyPath = args[0];
                var outputPath = args[1];
                var converter = new FunctionJsonConverter(logger, assemblyPath, outputPath);
                if (!converter.TryRun())
                {
                    logger.LogError("Error generating functions metadata");
                }
            }
        }
    }
}