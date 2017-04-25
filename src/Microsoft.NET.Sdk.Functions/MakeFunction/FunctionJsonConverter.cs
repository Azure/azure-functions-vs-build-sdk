using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Utilities;

namespace MakeFunctionJson
{
    internal class FunctionJsonConverter
    {
        private string _assemblyPath;
        private string _outputPath;
        private readonly FakeLogger _log;

        internal FunctionJsonConverter(string assemblyPath, string outputPath, TaskLoggingHelper log)
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
            if (!Path.IsPathRooted(_outputPath))
            {
                _outputPath = Path.Combine(Directory.GetCurrentDirectory(), _outputPath);
            }
            _log = new FakeLogger(log);
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
        internal bool TryRun()
        {
            try
            {
#if NET46
                var assembly = Assembly.LoadFrom(_assemblyPath);
#else
                var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(_assemblyPath);
#endif
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
                        else if (method.HasFunctionNameAttribute())
                        {
                            _log.LogWarning($"Method {method.Name} is missing a trigger attribute. Both a trigger attribute and FunctionName attribute are required for an Azure function definition.");
                        }
                        else if (method.HasWebJobSdkAttribute())
                        {
                            _log.LogWarning($"Method {method.Name} is missing the 'FunctionName' attribute. Both a trigger attribute and 'FunctionName' are required for an Azure function definition.");
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _log.LogErrorFromException(e);
                return false;
            }
        }
    }
}
