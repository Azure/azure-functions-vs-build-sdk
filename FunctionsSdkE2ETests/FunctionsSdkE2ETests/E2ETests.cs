using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FunctionsSdkE2ETests
{
    public class E2ETests
    {
        private const string _expectedExtensionsJson = "{\"extensions\":[{ \"name\": \"Startup\", \"typeName\":\"SharedStartup.Startup, SharedStartup, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\"}]}";
        private const string _expectedBinFolder = @"\bin";

        private void CleanBinFolders(string rootDir)
        {
            foreach (string binDir in Directory.EnumerateDirectories(rootDir, "bin", new EnumerationOptions { RecurseSubdirectories = true }))
            {
                Directory.Delete(binDir, true);
            }
        }

        [Fact]
        public void Build_DirectRef()
        {
            string solutionName = "DirectRef";

            string solutionFile = solutionName + ".sln";
            string workingDir = FindContainingDirectory(solutionFile);

            RunDotNet("restore", workingDir, solutionFile);
            RunDotNet("clean", workingDir, solutionFile);
            RunDotNet("build", workingDir, solutionFile);

            ValidateExtensionsJsonRecursive(Path.Combine(workingDir, solutionName), 1, expectedFolder: _expectedBinFolder,
                ValidateDirectRefStartupExtension,
                ValidateSharedStartupExtension);
        }

        [Fact]
        public void Publish_DirectRef()
        {
            string publishDir = Path.Combine(Path.GetTempPath(), "FunctionsSdkTests", "pub_directRef");
            if (Directory.Exists(publishDir))
            {
                Directory.Delete(publishDir, true);
            }

            string solutionName = "DirectRef";
            string solutionFile = solutionName + ".sln";
            string workingDir = FindContainingDirectory(solutionFile);

            RunDotNet("restore", workingDir, solutionFile);
            RunDotNet("clean", workingDir, solutionFile);
            RunDotNet("publish", workingDir, solutionFile, $"-o {publishDir} /bl");

            ValidateExtensionsJsonRecursive(publishDir, 1, expectedFolder: _expectedBinFolder,
                ValidateDirectRefStartupExtension,
                ValidateSharedStartupExtension);
        }

        [Fact]
        public void Build_NoSdkRef()
        {
            string solutionName = "NoSdkRef";

            string solutionFile = solutionName + ".sln";
            string workingDir = FindContainingDirectory(solutionFile);

            string projectDir = Path.Combine(workingDir, solutionName);

            CleanBinFolders(projectDir);

            RunDotNet("restore", workingDir, solutionFile);
            RunDotNet("clean", workingDir, solutionFile);
            RunDotNet("build", workingDir, solutionFile);

            ValidateExtensionsJsonRecursive(projectDir, 1, expectedFolder: @"Debug\netcoreapp2.1",
                t =>
                {
                    return t["name"].ToString() == "AzureStorage"
                        && t["typeName"].ToString() == "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage, Version=3.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
                });
        }
		
		[Fact]
        public void Build_V3()
        {
            RunBasicValidation("V3");
        }

        private void RunBasicValidation(string solutionName)
        {
            string solutionFile = solutionName + ".sln";
            string workingDir = FindContainingDirectory(solutionFile);

            string projectDir = Path.Combine(workingDir, solutionName);

            CleanBinFolders(projectDir);

            RunDotNet("restore", workingDir, solutionFile);
            RunDotNet("clean", workingDir, solutionFile);
            RunDotNet("build", workingDir, solutionFile);

            ValidateExtensionsJsonRecursive(projectDir, 1, expectedFolder: _expectedBinFolder,
                t =>
                {
                    return t["name"].ToString() == "AzureStorage"
                        && t["typeName"].ToString() == "Microsoft.Azure.WebJobs.Extensions.Storage.AzureStorageWebJobsStartup, Microsoft.Azure.WebJobs.Extensions.Storage, Version=3.0.10.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
                },
                t =>
                {
                    return t["name"].ToString() == "Startup"
                        && t["typeName"].ToString() == $"{solutionName}.Startup, {solutionName}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                });

            // The tests include one auto-created and one manually-created function.json file. The build should generate both into the output folder.
            string projectFolder = Path.GetDirectoryName(projectDir);
            string projectBinDir = Path.Combine(projectDir, solutionName, "bin");

            IEnumerable<string> functionJsonFilePaths = Directory.EnumerateFiles(Path.Combine(projectBinDir), "function.json", new EnumerationOptions { RecurseSubdirectories = true });
            // Comment out until 3.0.10 and 1.0.38 are released
            // Assert.Collection(functionJsonFilePaths,
            //     p => Assert.EndsWith("\\Function1\\function.json", p),
            //     p => Assert.EndsWith("\\Function2\\function.json", p));
        }

        private void RunTest(string solutionName, int expectedExtensionsJsonCount = 1)
        {
            string solutionFile = solutionName + ".sln";
            string workingDir = FindContainingDirectory(solutionFile);

            RunDotNet("restore", workingDir, solutionFile);
            RunDotNet("clean", workingDir, solutionFile);
            RunDotNet("build", workingDir, solutionFile);

            ValidateExtensionsJsonRecursive(Path.Combine(workingDir, solutionName), expectedExtensionsJsonCount);
        }

        private bool ValidateSharedStartupExtension(JToken extensionToken)
        {
            return extensionToken["name"].ToString() == "Startup" && extensionToken["typeName"].ToString() == "SharedStartup.Startup, SharedStartup, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        }

        private bool ValidateDirectRefStartupExtension(JToken extensionToken)
        {
            return extensionToken["name"].ToString() == "DirectRefStartup" && extensionToken["typeName"].ToString() == "DirectRefEMG.DirectRefStartup, DirectRefEMG, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        }

        private void ValidateExtensionsJsonRecursive(string startingDir, int expectedCount)
        {
            ValidateExtensionsJsonRecursive(startingDir, expectedCount, expectedFolder: _expectedBinFolder, ValidateSharedStartupExtension);
        }

        private void ValidateExtensionsJsonRecursive(string startingDir, int expectedCount, string expectedFolder, params Func<JToken, bool>[] extensionValidators)
        {
            // Check all extensions.json
            IEnumerable<string> extensionsFiles = Directory.EnumerateFiles(Path.Combine(startingDir), "extensions.json", new EnumerationOptions { RecurseSubdirectories = true });

            Assert.Equal(expectedCount, extensionsFiles.Count());

            foreach (string file in extensionsFiles)
            {
                Assert.True(Path.GetDirectoryName(file).EndsWith(expectedFolder, StringComparison.OrdinalIgnoreCase), $"'{file}' is not in the '{expectedFolder}' folder");

                JObject actualJson = JObject.Parse(File.ReadAllText(file));

                JToken[] extensionsArray = actualJson["extensions"]
                    .AsEnumerable()
                    .OrderBy(p => p["name"])
                    .ToArray();

                string createErrorMessage()
                {
                    return $"File: {file} | Actual: {actualJson}";
                }

                Assert.True(extensionValidators.Length == extensionsArray.Length, $"Incorrect number of validators ({extensionValidators.Length}) | {createErrorMessage()}");

                for (int i = 0; i < extensionValidators.Length; i++)
                {
                    JToken extension = extensionsArray[i];
                    Assert.True(extensionValidators[i](extension), $"Failed validator for {extension} | {createErrorMessage()}");
                }
            }
        }

        private void RunDotNet(string command, string workingDir, string solutionFile, string additionalArgs = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WorkingDirectory = workingDir,
                FileName = "dotnet",
                Arguments = $"{command} {solutionFile} {additionalArgs} -nodeReuse:False",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(startInfo))
            {
                StringBuilder stdOut = new StringBuilder(); ;
                StringBuilder stdErr = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        stdOut.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        stdErr.AppendLine(e.Data);
                    }
                };

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();

                Assert.True(process.ExitCode == 0, $"StdOut: {stdOut} | StdErr: {stdErr}");
                Assert.Empty(stdErr.ToString());
                //Assert.DoesNotContain(": warning ", stdOut.ToString());
                Assert.DoesNotContain(": error ", stdOut.ToString());
            }
        }

        private string FindContainingDirectory(string fileToFind)
        {
            string currentDir = Directory.GetCurrentDirectory();
            string dir = null;

            while (currentDir != null && dir == null)
            {
                if (Directory.EnumerateFiles(currentDir, fileToFind).SingleOrDefault() != null)
                {
                    dir = currentDir;
                }
                else
                {
                    currentDir = Directory.GetParent(currentDir)?.FullName;
                }
            }

            return dir;
        }
    }
}
