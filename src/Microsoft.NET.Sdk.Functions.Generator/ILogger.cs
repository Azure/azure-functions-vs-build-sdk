using System;

namespace MakeFunctionJson
{
    internal interface ILogger
    {
        void LogError(string message);
        void LogErrorFromException(Exception e);
        void LogWarning(string message);
        void LogWarningFromException(Exception e);
    }
}