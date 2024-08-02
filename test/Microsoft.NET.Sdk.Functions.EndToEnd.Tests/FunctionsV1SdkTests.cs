using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class FunctionsV1SdkTests
    {
        private string _testsDirectory;
        private string _msbuildPath;
        private string _nugetExePath;
        private bool _isTestPreReqSatisfied;
        private TestInitialize _testInitializer;
        private ITestOutputHelper _testOutputHelper;
        private const string _testVersion = "v1";
        private const string _msbuildLoggingVerbosity = "m"; 

        public FunctionsV1SdkTests(ITestOutputHelper testOutputHelper)
        {
            // Running the pack command is only needed for tests running against the latest version.
            _testInitializer = new TestInitialize(testOutputHelper, _testVersion, runPack: false);
            _testOutputHelper = testOutputHelper;
            _testsDirectory = _testInitializer.TestDirectory;
            _msbuildPath = GetMSBuildPath();
            _nugetExePath = GetNuGetPath();

            _isTestPreReqSatisfied = File.Exists(_msbuildPath) && File.Exists(_nugetExePath);
        }

        private string GetMSBuildPath()
        {
            var vswherePath = Environment.ExpandEnvironmentVariables($@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\{TestInitialize.VsWhere}");
            if (!File.Exists(vswherePath))
            {
                return null;
            }
            var args = $@"-latest -requires Microsoft.Component.MSBuild -find ""MSBuild\**\Bin\{TestInitialize.MSBuildExecutable}""";
            ProcessOutput readOutput = new ProcessOutput();
            int? exitCode = new ProcessWrapper().RunProcess(vswherePath, args, _testsDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: readOutput);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);
            return readOutput.StdOut?.TrimEnd('\r', '\n'); ;
        }

        private string GetNuGetPath()
        {
            string projectToRestore = "NuGet";
            string projectFilePath = Path.Combine(_testsDirectory, projectToRestore);
            string dotnetArgs = $"restore {projectToRestore}.csproj";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFilePath, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);
            return Environment.ExpandEnvironmentVariables($@"%userprofile%\.nuget\packages\nuget.commandline\5.6.0\tools\{TestInitialize.NuGetExecutable}");
        }

        /*
        [Theory]
        [InlineData("FunctionAppNETFramework", "FunctionAppNETFramework", TestInitialize.NetFramework)]
        [InlineData("FunctionAppNETStandard", "FunctionAppNETStandard", TestInitialize.NetStandard)]
        [InlineData("FunctionAppNETFxNETStandard", "FunctionAppNETFramework", TestInitialize.NetFramework)]
        public void BuildAndPublish_V1Functions(string solutionName, string projectName, string targetFramework)
        {
            if (!_isTestPreReqSatisfied)
            {
                return;
            }

            // Name of the csproj
            string solutionToTest = solutionName;
            string projectToTest = projectName;
            string solutionFileDirectory = Path.Combine(_testsDirectory, solutionToTest);
            string projectFileDirectory = Path.Combine(solutionFileDirectory, projectToTest);

            // Restore
            string exeArgs = $"restore {solutionToTest}.sln";
            int? exitCode = new ProcessWrapper().RunProcess(_nugetExePath, exeArgs, solutionFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

            // Build
            exeArgs = $"{solutionToTest}.sln /p:configuration={TestInitialize.Configuration} /v:{_msbuildLoggingVerbosity}";
            exitCode = new ProcessWrapper().RunProcess(_msbuildPath, exeArgs, solutionFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string additionalBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, targetFramework, "bin");
            Assert.True(Directory.Exists(additionalBinDir));
            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Publish
            exeArgs = $"{projectToTest}.csproj /t:Publish /p:configuration={TestInitialize.Configuration} /v:{_msbuildLoggingVerbosity}";
            exitCode = new ProcessWrapper().RunProcess(_msbuildPath, exeArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string additionalPublishBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, targetFramework, "publish", "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));
            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
        }
        */
    }
}
