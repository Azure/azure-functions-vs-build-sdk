using System;
using System.Collections.Generic;
using System.IO;

namespace MakeFunctionJson
{
    internal class BuildArtifactsLog
    {
        private const string buildArtifactsLogName = "functionsSdk.out";
        private readonly HashSet<string> _artifacts;
        private readonly string _logPath;

        public BuildArtifactsLog(string outputPath)
        {
            _artifacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _logPath = Path.Combine(outputPath, buildArtifactsLogName);
            try
            {
                if (File.Exists(_logPath))
                {
                    foreach (var line in File.ReadAllLines(_logPath))
                    {
                        _artifacts.Add(line);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Unable to read file {_logPath}");
                Logger.LogWarningFromException(e);
            }
        }

        public bool TryClearBuildArtifactsLog()
        {
            return Try(() => File.Delete(_logPath), $"Unable to clean file '{_logPath}'", true);
        }

        public bool TryAddBuildArtifact(string name)
        {
            return Try(() => File.AppendAllLines(_logPath, new[] { name }), $"Unable to update file '{_logPath}'", true);
        }

        public bool IsBuildArtifact(string name)
        {
            return _artifacts.Contains(name);
        }

        private bool Try(Action action, string message, bool isError)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                if (isError)
                {
                    Logger.LogError(message);
                    Logger.LogErrorFromException(e);
                }
                else
                {
                    Logger.LogWarning(message);
                    Logger.LogWarningFromException(e);
                }
                return !isError;
            }
        }
    }
}