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
            _outputPath = outputPath.Trim('"');
        }

        /// <summary>
        /// Run loads assembly in <see cref="_assemblyPath"/> then:
        /// |> GetExportedTypes
        /// |> GetMethods
        /// |> Where method is SDK method
        /// |> Convert that to function.json
        /// |> Create folder \{functionName}\
        /// |> Write \{functionName}\function.json
        /// 
        /// This means that every <see cref="MethodInfo"/> will be N binding objects on <see cref="FunctionJsonSchema"/>
        /// Where N == total number of SDK attributes on the method parameters.
        /// </summary>
        internal void Run()
        {
            var assembly = Assembly.LoadFrom(_assemblyPath);
            // MakeRelativePath takes 2 paths and returns a relative path from first to second.
            // Since function.json for each function will live in a sub directory of the _outputPath
            // we need to send a sub-directory in for the first parameter. Hence the Path.Combine()
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
