using System.Collections.Generic;
using Microsoft.NET.Sdk.Functions.Console;
using Xunit;

namespace Microsoft.NET.Sdk.Functions.Test
{
    public class ArgumentsTests
    {
        [Fact]
        public void NoExcludedFunctions()
        {
            var args = new[] { @"assemblyPath", "outputPath", "true" };

            Program.ParseArgs(args, out string assemblyPath, out string outputPath, out bool functionsInDependencies, out IEnumerable<string> excludedFunctionNames);
            Assert.Equal("assemblyPath", assemblyPath);
            Assert.Equal("outputPath", outputPath);
            Assert.True(functionsInDependencies);
            Assert.Empty(excludedFunctionNames);
        }

        [Fact]
        public void ExcludedFunctions()
        {
            var args = new[] { @"assemblyPath", "outputPath", "false", "Function1;Function2" };

            Program.ParseArgs(args, out string assemblyPath, out string outputPath, out bool functionsInDependencies, out IEnumerable<string> excludedFunctionNames);
            Assert.Equal("assemblyPath", assemblyPath);
            Assert.Equal("outputPath", outputPath);
            Assert.False(functionsInDependencies);
            Assert.Collection(excludedFunctionNames,
                s => Assert.Equal("Function1", s),
                s => Assert.Equal("Function2", s));
        }
    }
}
