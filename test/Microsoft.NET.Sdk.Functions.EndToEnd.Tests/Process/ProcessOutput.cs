using System.Text;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class ProcessOutput : ITestOutputHelper, IReadProcessOutput
    {
        private StringBuilder _stringBuilder;

        public ProcessOutput()
        {
            _stringBuilder = new StringBuilder(256);
        }

        public string StdOut { get => _stringBuilder.ToString(); }

        public void WriteLine(string message)
        {
            _stringBuilder.Append(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _stringBuilder.AppendFormat(format, args);
        }
    }
}
