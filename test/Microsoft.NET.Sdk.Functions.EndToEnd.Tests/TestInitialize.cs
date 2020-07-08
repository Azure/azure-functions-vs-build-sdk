using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class TestInitialize : IDisposable
    {
        public const string TestProjectsSourceDirectory = "Resources";
        public const string TestProjectsTargetDirectory = "TestResults";
        public const string DotNetExeName = "dotnet";
        public const string FunctionsNetSdkProject = "Microsoft.Net.Sdk.Functions";
        public const string FunctionsMsBuildProject = "Microsoft.NET.Sdk.Functions.MSBuild";
        public const string FunctionsGeneratorProject = "Microsoft.NET.Sdk.Functions.Generator";
        public const string Configuration = "Release";
        public const string Framework = "netcoreapp3.1";

        public static readonly string PathToRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
        public static readonly string SrcRoot = Path.Combine(PathToRepoRoot, "src");
        public static readonly string PackRoot = Path.Combine(PathToRepoRoot, "pack");
        public static readonly string TestRoot = Path.Combine(PathToRepoRoot, "test");

        public string PackageSource { get; private set; }
        public string TestDirectory { get; private set; }

        public TestInitialize()
        {
            ITestOutputHelper testOutputHelper = null;
            // Delete the package cache
            string packageCachepath = Environment.ExpandEnvironmentVariables(@"%userprofile%\.nuget\packages\microsoft.net.sdk.functions");
            if (Directory.Exists(packageCachepath))
            {
                Directory.Delete(packageCachepath, true);
            }

            // Run dotnet restore at solution root.
            string dotnetArgs = $"restore";
            int? exitCode = new ProcessWrapper().RunProcess(DotNetExeName, dotnetArgs, PathToRepoRoot, out int? _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);

            // Build the functions msbuild project.
            string projectDir = Path.Combine(SrcRoot, FunctionsMsBuildProject);
            dotnetArgs = $"build --configuration {Configuration}";
            exitCode = new ProcessWrapper().RunProcess(DotNetExeName, dotnetArgs, projectDir, out int? _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);

            // Build the functions generator project.
            projectDir = Path.Combine(SrcRoot, FunctionsGeneratorProject);
            exitCode = new ProcessWrapper().RunProcess(DotNetExeName, dotnetArgs, projectDir, out _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);

            // Create the package
            projectDir = Path.Combine(PackRoot, FunctionsNetSdkProject);
            dotnetArgs = $"pack --configuration {Configuration}";
            exitCode = new ProcessWrapper().RunProcess(DotNetExeName, dotnetArgs, projectDir, out _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
            Debug.Assert(exitCode.HasValue && exitCode.Value == 0);

            // Setup the package source.
            var packageDir = Path.Combine(projectDir, "bin", Configuration);
            var nupkg = Directory.EnumerateFiles(packageDir, "*.nupkg", SearchOption.TopDirectoryOnly);
            Debug.Assert(nupkg.Single() != null);
            PackageSource = Path.GetDirectoryName(nupkg.Single());

            // Setup the test directory.
            string sourceDirectory = Path.Combine(AppContext.BaseDirectory, TestProjectsSourceDirectory);
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            string targetDirectory = Path.Combine(PathToRepoRoot, TestProjectsTargetDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
            CopyAll(diSource, diTarget);
            TestDirectory = targetDirectory;
        }

        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public void Dispose()
        {
            // ... clean up
        }
    }
}
