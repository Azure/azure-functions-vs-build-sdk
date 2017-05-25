using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace MakeFunctionJson
{
    internal class FunctionJsonConverter
    {
        private string _assemblyPath;
        private string _outputPath;
        private readonly FakeLogger _log;
        private readonly IDictionary<string, MethodInfo> _functionNamesSet;
        private readonly BuildArtifactsLog _buildArtifactsLog;
        private static readonly IEnumerable<string> _functionsArtifacts = new[]
        {
            "local.settings.json",
            "host.json"
        };

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
            _functionNamesSet = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
            _buildArtifactsLog = new BuildArtifactsLog(_outputPath, _log);
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
                this._functionNamesSet.Clear();
                if (!_buildArtifactsLog.TryClearBuildArtifactsLog())
                {
                    _log.LogError("Unable to clean build artifacts file.");
                    return false;
                }

                CleanOutputPath();

                return TryGenerateFunctionJsons() && TryCopyFunctionArtifacts();
            }
            catch (Exception e)
            {
                _log.LogErrorFromException(e);
                return false;
            }
        }

        private bool TryCopyFunctionArtifacts()
        {
            var sourceFile = string.Empty;
            var targetFile = string.Empty;
            try
            {
                var assemblyDir = Path.GetDirectoryName(_assemblyPath);
                foreach (var file in _functionsArtifacts)
                {
                    sourceFile = Path.Combine(assemblyDir, file);
                    targetFile = Path.Combine(_outputPath, file);
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, targetFile, overwrite: true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _log.LogError($"Unable to copy '{sourceFile}' to '{targetFile}'");
                _log.LogErrorFromException(e);
                return false;
            }
        }

        private bool TryGenerateFunctionJsons()
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
                        var artifactName = Path.Combine(functionName, "function.json");
                        var path = Path.Combine(_outputPath, artifactName);
                        if (!File.Exists(path) &&
                            CheckAppSettingsAndFunctionName(functionJson, method) &&
                            _buildArtifactsLog.TryAddBuildArtifact(artifactName))
                        {
                            functionJson.Serialize(path);
                        }
                        else
                        {
                            return false;
                        }
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

        private bool CheckAppSettingsAndFunctionName(FunctionJsonSchema functionJson, MethodInfo method)
        {
            try
            {
                var functionName = method.GetSdkFunctionName();
                if (this._functionNamesSet.ContainsKey(functionName))
                {
                    var dupMethod = this._functionNamesSet[functionName];
                    this._log.LogError($"Function {method.DeclaringType.FullName}.{method.Name} and {dupMethod.DeclaringType.FullName}.{dupMethod.Name} have the same value for FunctionNameAttribute. Each function must have a unique name.");
                    return false;
                }
                else
                {
                    this._functionNamesSet.Add(functionName, method);
                }

                const string settingsFileName = "local.settings.json";
                var settingsFile = Path.Combine(_outputPath, settingsFileName);

                if (!File.Exists(settingsFile))
                {
                    return true; // no file to check.
                }

                var settings = JsonConvert.DeserializeObject<LocalSettingsJson>(File.ReadAllText(settingsFile));
                var values = settings?.Values;

                if (values != null)
                {
                    // FirstOrDefault returns a KeyValuePair<string, string> which is a struct so it can't be null.
                    var azureWebJobsStorage = values.FirstOrDefault(pair => pair.Key.Equals("AzureWebJobsStorage", StringComparison.OrdinalIgnoreCase)).Value;
                    var isHttpTrigger = functionJson
                        .Bindings
                        .Where(b => b["type"] != null)
                        .Select(b => b["type"].ToString())
                        .Where(b => b.IndexOf("Trigger") != -1)
                        .Any(t => t == "httpTrigger");

                    if (string.IsNullOrWhiteSpace(azureWebJobsStorage) && !isHttpTrigger)
                    {
                        _log.LogWarning($"Function [{functionName}]: Missing value for AzureWebJobsStorage in {settingsFileName}. This is required for all triggers other than HTTP.");
                    }

                    foreach (var binding in functionJson.Bindings)
                    {
                        foreach (var token in binding)
                        {
                            if (token.Key == "connection" || token.Key == "apiKey" || token.Key == "accountSid" || token.Key == "authToken")
                            {
                                var appSettingName = token.Value.ToString();
                                if (!values.Any(v => v.Key.Equals(appSettingName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    _log.LogWarning($"Function [{functionName}]: cannot find value named '{appSettingName}' in {settingsFileName} that matches '{token.Key}' property set on '{binding["type"]?.ToString()}'");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogWarningFromException(e);
            }
            // We only return false on an error, not a warning.
            return true;
        }

        private void CleanOutputPath()
        {
            Directory.GetDirectories(_outputPath)
                .Select(d => Path.Combine(d, "function.json"))
                .Where(File.Exists)
                .Where(f => _buildArtifactsLog.IsBuildArtifact(f.Replace(_outputPath, string.Empty)))
                .Select(Path.GetDirectoryName)
                .ToList()
                .ForEach(directory =>
                {
                    try
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                    catch
                    {
                        _log.LogWarning($"Unable to clean directory {directory}.");
                    }
                });
        }
    }
}
