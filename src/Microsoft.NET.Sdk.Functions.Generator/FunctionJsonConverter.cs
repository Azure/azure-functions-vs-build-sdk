﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace MakeFunctionJson
{
    internal class FunctionJsonConverter
    {
        private string _assemblyPath;
        private string _outputPath;
        private bool _functionsInDependencies;
        private readonly HashSet<string> _excludedFunctionNames;
        private readonly ILogger _logger;
        private readonly IDictionary<string, MethodInfo> _functionNamesSet;

        private static readonly IEnumerable<string> _functionsArtifacts = new[]
        {
            "local.settings.json",
            "host.json"
        };

        private static readonly IEnumerable<string> _triggersWithoutStorage = new[]
        {
            "httptrigger",
            "kafkatrigger"
        };

        internal FunctionJsonConverter(ILogger logger, string assemblyPath, string outputPath, bool functionsInDependencies, IEnumerable<string> excludedFunctionNames = null)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            _logger = logger;
            _assemblyPath = assemblyPath;
            _outputPath = outputPath.Trim('"');
            _functionsInDependencies = functionsInDependencies;
            _excludedFunctionNames = new HashSet<string>(excludedFunctionNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (!Path.IsPathRooted(_outputPath))
            {
                _outputPath = Path.Combine(Directory.GetCurrentDirectory(), _outputPath);
            }
            _functionNamesSet = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
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
                CleanOutputPath();
                CopyFunctionArtifacts();

                return TryGenerateFunctionJsons();
            }
            catch (Exception e)
            {
                _logger.LogErrorFromException(e);
                return false;
            }
        }

        private void CopyFunctionArtifacts()
        {
            var sourceFile = string.Empty;
            var targetFile = string.Empty;

            var assemblyDir = Path.GetDirectoryName(_assemblyPath);
            foreach (var file in _functionsArtifacts)
            {
                sourceFile = Path.Combine(assemblyDir, file);
                targetFile = Path.Combine(_outputPath, file);
                if (File.Exists(sourceFile) && !sourceFile.Equals(targetFile, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Copy(sourceFile, targetFile, overwrite: true);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning($"Unable to copy '{sourceFile}' to '{targetFile}'");
                        _logger.LogWarningFromException(e);
                    }
                }
            }
        }

        public IEnumerable<(FunctionJsonSchema schema, FileInfo outputFile)?> GenerateFunctions(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.HasFunctionNameAttribute())
                    {
                        if (method.HasUnsuportedAttributes(out string error))
                        {
                            _logger.LogError(error);
                            yield return null;
                        }
                        else if (method.IsWebJobsSdkMethod())
                        {
                            var functionName = method.GetSdkFunctionName();
                            var artifactName = Path.Combine(functionName, "function.json");
                            var path = Path.Combine(_outputPath, artifactName);
                            var relativeAssemblyPath = PathUtility.MakeRelativePath(Path.Combine(_outputPath, "dummyFunctionName"), type.Assembly.Location);
                            var functionJson = method.ToFunctionJson(relativeAssemblyPath);
                            if (CheckAppSettingsAndFunctionName(functionJson, method))
                            {
                                yield return (functionJson, new FileInfo(path));
                            }
                            else
                            {
                                yield return null;
                            }
                        }
                        else if (method.HasFunctionNameAttribute())
                        {
                            if (method.HasNoAutomaticTriggerAttribute() && method.HasTriggerAttribute())
                            {
                                _logger.LogWarning($"Method {method.ReflectedType?.FullName}.{method.Name} has both a 'NoAutomaticTrigger' attribute and a trigger attribute. Both can't be used together for an Azure function definition.");
                            }
                            else
                            {
                                _logger.LogWarning($"Method {method.ReflectedType?.FullName}.{method.Name} is missing a trigger attribute. Both a trigger attribute and FunctionName attribute are required for an Azure function definition.");
                            }
                        }
                        else if (method.HasValidWebJobSdkTriggerAttribute())
                        {
                            _logger.LogWarning($"Method {method.ReflectedType?.FullName}.{method.Name} is missing the 'FunctionName' attribute. Both a trigger attribute and 'FunctionName' are required for an Azure function definition.");
                        }
                    }
                }
            }
        }
        
        private bool TryGenerateFunctionJsons()
        {
            var assembly = Assembly.LoadFrom(_assemblyPath);
            var assemblyRoot = Path.GetDirectoryName(_assemblyPath);

            var exportedTypes = assembly.ExportedTypes;

            if (_functionsInDependencies)
            {
                foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    var tryPath = Path.Combine(assemblyRoot, $"{referencedAssembly.Name}.dll");
                    try
                    {
                        var loadedAssembly = Assembly.LoadFrom(tryPath);
                        exportedTypes = exportedTypes.Concat(loadedAssembly.ExportedTypes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not evaluate '{referencedAssembly.Name}' for function types. Exception message: {ex.Message}");
                    }
                }
            }

            var functions = GenerateFunctions(exportedTypes).ToList();
            foreach (var function in functions.Where(f => f.HasValue && !f.Value.outputFile.Exists).Select(f => f.Value))
            {
                function.schema.Serialize(function.outputFile.FullName);
            }
            return functions.All(f => f.HasValue);
        }

        private bool CheckAppSettingsAndFunctionName(FunctionJsonSchema functionJson, MethodInfo method)
        {
            try
            {
                var functionName = method.GetSdkFunctionName();
                if (this._functionNamesSet.ContainsKey(functionName))
                {
                    var dupMethod = this._functionNamesSet[functionName];
                    _logger.LogError($"Function {method.DeclaringType.FullName}.{method.Name} and {dupMethod.DeclaringType.FullName}.{dupMethod.Name} have the same value for FunctionNameAttribute. Each function must have a unique name.");
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
                    var allWithoutStorageTriggers = functionJson
                        .Bindings
                        .Where(b => b["type"] != null)
                        .Select(b => b["type"].ToString())
                        .Where(b => b.IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) != -1)
                        .All(t => _triggersWithoutStorage.Any(tws => tws.Equals(t, StringComparison.OrdinalIgnoreCase)));

                    if (string.IsNullOrWhiteSpace(azureWebJobsStorage) && !allWithoutStorageTriggers)
                    {
                        _logger.LogWarning($"Function [{functionName}]: Missing value for AzureWebJobsStorage in {settingsFileName}. " +
                            $"This is required for all triggers other than {string.Join(", ", _triggersWithoutStorage)}.");
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
                                    _logger.LogWarning($"Function [{functionName}]: cannot find value named '{appSettingName}' in {settingsFileName} that matches '{token.Key}' property set on '{binding["type"]?.ToString()}'");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarningFromException(e);
            }
            // We only return false on an error, not a warning.
            return true;
        }

        private void CleanOutputPath()
        {
            Directory.GetDirectories(_outputPath)
                .Select(d => Path.Combine(d, "function.json"))
                .Where(File.Exists)
                .Select(Path.GetDirectoryName)
                .Where(d => !_excludedFunctionNames.Contains(new DirectoryInfo(d).Name))
                .ToList()
                .ForEach(directory =>
                {
                    try
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                    catch
                    {
                        _logger.LogWarning($"Unable to clean directory {directory}.");
                    }
                });
        }
    }
}