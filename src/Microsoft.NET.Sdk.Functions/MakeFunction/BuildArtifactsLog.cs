using System;
using System.Collections.Generic;
using System.IO;

namespace MakeFunctionJson
{
    internal class BuildArtifactsLog
    {
        private const string buildArtifactsLogName = "functionsSdk.out";
        private readonly HashSet<string> _artifacts;
        private readonly FakeLogger _logger;
        private readonly string _logPath;

        public BuildArtifactsLog(string outputPath, FakeLogger logger)
        {
            _logger = logger;
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
                _logger.LogWarning($"Unable to read file {_logPath}");
                _logger.LogWarningFromException(e);
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
                    _logger.LogError(message);
                    _logger.LogErrorFromException(e);
                }
                else
                {
                    _logger.LogWarning(message);
                    _logger.LogWarningFromException(e);
                }
                return !isError;
            }
        }
    }
}
