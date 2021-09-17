using System;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Functions.EndToEnd.Tests
{
    public class TestInitialize
    {
        public const string TestProjectsSourceDirectory = "Resources";
        public const string TestProjectsTargetDirectory = "TestResults";
        // Source Project names
        public const string FunctionsNetSdkProject = "Microsoft.Net.Sdk.Functions";
        public const string FunctionsMsBuildProject = "Microsoft.NET.Sdk.Functions.MSBuild";
        public const string FunctionsGeneratorProject = "Microsoft.NET.Sdk.Functions.Generator";
        // Configurations
        public const string Configuration = "Debug";
        public const string NetCoreFramework = "net6.0";
        public const string NetFramework = "net461";
        public const string NetStandard = "netstandard2.0";

        // NuGet Sources
        public const string NuGetPackageSource = @"https://api.nuget.org/v3/index.json";
        // Paths and executables
        public static readonly string DotNetExecutable = "dotnet";
        public static readonly string MSBuildExecutable = "msbuild.exe";
        public static readonly string NuGetExecutable = "nuget.exe";
        public static readonly string VsWhere = "vswhere.exe";

        public static readonly string PathToRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
        public static readonly string SrcRoot = Path.Combine(PathToRepoRoot, "src");
        public static readonly string TestRoot = Path.Combine(PathToRepoRoot, "test");

        public string FunctionsSdkPackageSource { get; private set; }
        public string TestDirectory { get; private set; }

        public TestInitialize(ITestOutputHelper testOutputHelper, string testVersion, bool runRestore = false, bool runBuild = false, bool runPack = true)
        {
            string dotnetArgs;
            int? exitCode;
            string projectDir = Path.Combine(SrcRoot, FunctionsNetSdkProject);

            if (runRestore)
            {
                // Run dotnet restore at solution root.
                dotnetArgs = $"restore";
                exitCode = new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, PathToRepoRoot, out int? _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
                Debug.Assert(exitCode.HasValue && exitCode.Value == 0);
            }

            if (runBuild)
            {
                dotnetArgs = $"build --configuration {Configuration}";

                // Build the functions msbuild project.
                projectDir = Path.Combine(SrcRoot, FunctionsMsBuildProject);
                exitCode = new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectDir, out int? _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
                Debug.Assert(exitCode.HasValue && exitCode.Value == 0);

                // Build the functions generator project.
                projectDir = Path.Combine(SrcRoot, FunctionsGeneratorProject);
                exitCode = new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectDir, out _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);
                Debug.Assert(exitCode.HasValue && exitCode.Value == 0);
            }

            // Setup the test directory.
            string sourceDirectory = Path.Combine(AppContext.BaseDirectory, TestProjectsSourceDirectory, testVersion);
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);

            string targetDirectory = Path.Combine(PathToRepoRoot, TestProjectsTargetDirectory, testVersion);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            // Setup the package source.
            FunctionsSdkPackageSource = Path.Combine(targetDirectory, "packages") + Path.DirectorySeparatorChar;

            CopyAll(diSource, diTarget);
            TestDirectory = targetDirectory;

            if (runPack)
            {
                // Pack to the test directory with a unique name
                string version = DateTime.UtcNow.ToString("yyMMddHHmmss");
                dotnetArgs = $"pack --configuration {Configuration} -o {FunctionsSdkPackageSource} -p:BuildNumber={version}";

                // Create the package
                projectDir = Path.Combine(SrcRoot, FunctionsNetSdkProject);
                exitCode = new ProcessWrapper().RunProcess(DotNetExecutable, dotnetArgs, projectDir, out _, createDirectoryIfNotExists: false, testOutputHelper: testOutputHelper);

                Debug.Assert(exitCode.HasValue && exitCode.Value == 0);
            }           
        }

        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (target.Exists)
            {
                // try our best to clean up
                try
                {
                    target.Delete(true);
                }
                catch
                {
                }
            }

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
    }
}
