using System;
using System.Collections.Generic;
using MakeFunctionJson;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class RecorderLogger : ILogger
    {
        public List<object> Errors { get; } = new List<object>();
        public List<object> Warnings { get; } = new List<object>();
            
        public void LogError(string message)
        {
            Errors.Add(message);
        }

        public void LogErrorFromException(Exception e)
        {
            Errors.Add(e);
        }

        public void LogWarning(string message)
        {
            Warnings.Add(message);
        }

        public void LogWarningFromException(Exception e)
        {
            Warnings.Add(e);
        }
    }
}