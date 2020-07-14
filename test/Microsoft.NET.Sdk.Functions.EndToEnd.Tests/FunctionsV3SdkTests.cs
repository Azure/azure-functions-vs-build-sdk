using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class FunctionsV3SdkTests
    {
        private string _functionsSdkPackageSource;
        private string _testsDirectory;
        private TestInitialize _testInitializer;
        private ITestOutputHelper _testOutputHelper;
        private const string _testVersion = "v3";

        public FunctionsV3SdkTests(ITestOutputHelper testOutputHelper)
        {
            _testInitializer = new TestInitialize(testOutputHelper, _testVersion);
            _testOutputHelper = testOutputHelper;
            _functionsSdkPackageSource = _testInitializer.FunctionsSdkPackageSource;
            _testsDirectory = _testInitializer.TestDirectory;
        }

        [Fact]
        public void BuildAndPublish_CopiesBinariesToAdditionalBinFolder()
        {
            // Name of the csproj
            string projectFileToTest = "FunctionsSdk";
            string projectFileDirectory = Path.Combine(_testsDirectory, projectFileToTest);

            // Restore
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {_functionsSdkPackageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            string additionalBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework, "bin");
            Assert.True(Directory.Exists(additionalBinDir));
            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string additionalPublishBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework, "publish", "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));
            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
        }

        [Fact]
        public void BuildAndPublish_CopiesRuntimesToAdditionalBinFolder()
        {
            // Name of the csproj
            string projectFileToTest = "FunctionsSdkWithSQLClient";
            string projectFileDirectory = Path.Combine(_testsDirectory, projectFileToTest);

            // Restore
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {TestInitialize.NuGetPackageSource};{_functionsSdkPackageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string additionalBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework, "bin");
            Assert.True(Directory.Exists(additionalBinDir));
            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
            // Test addional runtimes
            files = Directory.EnumerateFiles(Path.Combine(additionalBinDir, "runtimes"), "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string additionalPublishBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework, "publish", "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));
            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
            // Test additional runtimes
            files = Directory.EnumerateFiles(Path.Combine(additionalPublishBinDir, "runtimes"), "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
        }

        [Fact]
        public void BuildAndPublish_GeneratesFunctions()
        {
            // Name of the csproj
            string projectFileToTest = "FunctionAppWithHttpTrigger";
            string projectFileDirectory = Path.Combine(_testsDirectory, projectFileToTest);

            // Restore
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {_functionsSdkPackageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test addional bin
            string binDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework);
            string additionalBinDir = Path.Combine(binDir, "bin");
            Assert.True(Directory.Exists(additionalBinDir));
            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
            // Test functions generator output
            string httpTriggerFunctionpath = Path.Combine(binDir, "HttpFunction", "function.json");
            Assert.True(File.Exists(httpTriggerFunctionpath));

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExecutable, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            Assert.True(exitCode.HasValue && exitCode.Value == 0);
            // Test additional bin
            string publishDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.NetCoreFramework, "publish");
            string additionalPublishBinDir = Path.Combine(publishDir, "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));
            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
            // Test functions generator output
            httpTriggerFunctionpath = Path.Combine(publishDir, "HttpFunction", "function.json");
            Assert.True(File.Exists(httpTriggerFunctionpath));
        }
    }
}
