using System;
using System.IO;
using System.Reflection;

namespace MakeFunctionJson
{
    internal class FunctionJsonConverter
    {
        private string _assemblyPath;
        private string _outputPath;

        internal FunctionJsonConverter(string assemblyPath, string outputPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            _assemblyPath = assemblyPath;
            _outputPath = outputPath;
        }

        internal void Run()
        {
            var assembly = Assembly.LoadFrom(_assemblyPath);
            var relativeAssemblyPath = PathUtility.MakeRelativePath(Path.Combine(_outputPath, "dummyFunctionName"), assembly.Location);
            foreach (var type in assembly.GetExportedTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.IsWebJobsSdkMethod())
                    {
                        var functionJson = method.ToFunctionJson(relativeAssemblyPath);
                        var functionName = method.GetSdkFunctionName();
                        var path = Path.Combine(_outputPath, functionName, "function.json");
                        functionJson.Serialize(path);
                    }
                }
            }
        }
    }
}
