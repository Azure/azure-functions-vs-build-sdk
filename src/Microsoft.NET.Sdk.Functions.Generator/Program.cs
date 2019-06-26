﻿using System;
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
                var assemblyPath = args[0].Trim();
                var outputPath = args[1].Trim();
                var functionsInDependencies = bool.Parse(args[2].Trim());

                IEnumerable<string> excludedFunctionNames = Enumerable.Empty<string>();

                if (args.Length > 2)
                {
                    var excludedFunctionNamesArg = args[2].Trim();
                    excludedFunctionNames = excludedFunctionNamesArg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }

                var converter = new FunctionJsonConverter(logger, assemblyPath, outputPath, functionsInDependencies, excludedFunctionNames);
                if (!converter.TryRun())
                {
                    logger.LogError("Error generating functions metadata");
                }
            }
        }
    }
}