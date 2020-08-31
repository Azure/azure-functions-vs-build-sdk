using System;
using System.Collections.Generic;
using System.Linq;
using MakeFunctionJson;

namespace Microsoft.NET.Sdk.Functions.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var logger = new ConsoleLogger();
            if (args.Length < 3 || args.Length > 4)
            {
                logger.LogError("USAGE: <assemblyPath> <outputPath> <functionsInDependencies> <excludedFunctionName1;excludedFunctionName2;...>");
            }
            else
            {
                ParseArgs(args, out string assemblyPath, out string outputPath, out bool functionsInDependencies, out IEnumerable<string> excludedFunctionNames);

                var converter = new FunctionJsonConverter(logger, assemblyPath, outputPath, functionsInDependencies, excludedFunctionNames);
                if (!converter.TryRun())
                {
                    logger.LogError("Error generating functions metadata");
                }
            }
        }

        internal static void ParseArgs(string[] args, out string assemblyPath, out string outputPath, out bool functionsInDependencies, out IEnumerable<string> excludedFunctionNames)
        {
            assemblyPath = args[0].Trim();
            outputPath = args[1].Trim();
            functionsInDependencies = bool.Parse(args[2].Trim());

            excludedFunctionNames = Enumerable.Empty<string>();

            if (args.Length > 3)
            {
                var excludedFunctionNamesArg = args[3].Trim();
                excludedFunctionNames = excludedFunctionNamesArg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
