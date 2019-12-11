using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
                var assemblyDir = Path.GetDirectoryName(assemblyPath);
                var outputPath = args[1].Trim();
                var functionsInDependencies = bool.Parse(args[2].Trim());

                AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
                {
                    string assemblyPath = Path.Combine(assemblyDir, assemblyName.Name + ".dll");

                    if (File.Exists(assemblyPath))
                    {
                        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                        return assembly;
                    }

                    try
                    {
                        // If the assembly file is not found, it may be a runtime assembly for a different
                        // runtime version (i.e. the Function app assembly targets .NET Core 3.1, yet this
                        // process is running 3.0). In that case, just try to return the currently-loaded assembly,
                        // even if it's the wrong version; we won't be running it, just reflecting.
                        Assembly assembly = Assembly.Load(assemblyName.Name);
                        return assembly;
                    }
                    catch (Exception)
                    {
                        // We'll already log an error if this happens; this gives a little more details if debug is enabled.
                        logger.LogError($"Unable to find fallback for assembly '{assemblyName}'.");
                        return null;
                    }
                };

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