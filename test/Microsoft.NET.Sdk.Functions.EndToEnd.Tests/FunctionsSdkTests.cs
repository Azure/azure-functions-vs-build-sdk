﻿using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class FunctionsSdkTests: IClassFixture<TestInitialize>
    {
        private string _packageSource;
        private string _testsDirectory;
        private ITestOutputHelper _testOutputHelper;
        public FunctionsSdkTests(ITestOutputHelper output, TestInitialize testInitialize)
        {
            _testOutputHelper = output;
            _packageSource = testInitialize.PackageSource;
            _testsDirectory = testInitialize.TestDirectory;
        }

        [Fact]
        public void BuildAndPublish_CopiesBinariesToAdditionalBinFolder()
        {
            // Name of the csproj
            string projectFileToTest = "FunctionsSdk";
            string projectFileDirectory = Path.Combine(_testsDirectory, projectFileToTest);

            // Restore
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {_packageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);
            
            string additionalBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework, "bin");
            Assert.True(Directory.Exists(additionalBinDir));

            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            string additionalPublishBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework, "publish", "bin");
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
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {_packageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            string additionalBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework, "bin");
            Assert.True(Directory.Exists(additionalBinDir));

            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            files = Directory.EnumerateFiles(Path.Combine(additionalBinDir, "runtimes"), "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            string additionalPublishBinDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework, "publish", "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));

            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            files = Directory.EnumerateFiles(Path.Combine(additionalPublishBinDir, "runtimes"), "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);
        }

        [Fact]
        public void Build_FunctionAppWithHttpTrigger_GeneratedFunction()
        {
            // Name of the csproj
            string projectFileToTest = "FunctionAppWithHttpTrigger";
            string projectFileDirectory = Path.Combine(_testsDirectory, projectFileToTest);

            // Restore
            string dotnetArgs = $"restore {projectFileToTest}.csproj --source {_packageSource}";
            int? exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            // Build
            dotnetArgs = $"build {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            string binDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework);
            string additionalBinDir = Path.Combine(binDir, "bin");
            Assert.True(Directory.Exists(additionalBinDir));
            var files = Directory.EnumerateFiles(additionalBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            // Check if the http function is generated
            string httpTriggerFunctionpath = Path.Combine(binDir, "HttpFunction", "function.json");
            Assert.True(File.Exists(httpTriggerFunctionpath));

            // Publish
            dotnetArgs = $"publish {projectFileToTest}.csproj --configuration {TestInitialize.Configuration}";
            exitCode = new ProcessWrapper().RunProcess(TestInitialize.DotNetExeName, dotnetArgs, projectFileDirectory, out int? _, createDirectoryIfNotExists: false, testOutputHelper: _testOutputHelper);

            string publishDir = Path.Combine(projectFileDirectory, "bin", TestInitialize.Configuration, TestInitialize.Framework, "publish");
            string additionalPublishBinDir = Path.Combine(publishDir, "bin");
            Assert.True(Directory.Exists(additionalPublishBinDir));

            files = Directory.EnumerateFiles(additionalPublishBinDir, "*.dll", SearchOption.AllDirectories);
            Assert.True(files.Count() > 1);

            httpTriggerFunctionpath = Path.Combine(publishDir, "HttpFunction", "function.json");
            Assert.True(File.Exists(httpTriggerFunctionpath));
        }
    }
}