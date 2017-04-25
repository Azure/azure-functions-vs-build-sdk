using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Utilities;

namespace MakeFunctionJson
{
    internal class FakeLogger
    {
        private readonly TaskLoggingHelper _log;

        public FakeLogger(TaskLoggingHelper log)
        {
            _log = log;
        }

        public void LogError(string message)
        {
            if (_log != null)
            {
                _log.LogError(message);
            }
            else
            {
                Console.WriteLine($"ERROR: {message}");
            }
        }

        public void LogErrorFromException(Exception e)
        {
            if (_log != null)
            {
                _log.LogErrorFromException(e);
            }
            else
            {
                Console.WriteLine($"ERROR: {e}");
            }
        }

        public void LogWarning(string message)
        {
            if (_log != null)
            {
                _log.LogWarning(message);
            }
            else
            {
                Console.WriteLine($"WARNING: {message}");
            }
        }
    }
}
