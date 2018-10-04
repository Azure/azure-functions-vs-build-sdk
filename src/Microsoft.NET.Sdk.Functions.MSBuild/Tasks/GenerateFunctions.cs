using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.Tasks
{
#if NET46
    [LoadInSeparateAppDomain]
    public class GenerateFunctions : AppDomainIsolatedTask
#else

    public class GenerateFunctions : Task
#endif
    {
        [Required]
        public string TargetPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        private const string NETFrameworkFolder = "net46";
        private const string NETStandardFolder = "netcoreapp2.1";

        public bool UseNETCoreGenerator { get; set; }

        public bool UseNETFrameworkGenerator { get; set; }

        public bool GenerateHostJson { get; set; }

        public ITaskItem[] UserProvidedFunctionJsonFiles { get; set; }

        public override bool Execute()
        {
            string hostJsonPath = Path.Combine(OutputPath, "host.json");
            if (GenerateHostJson && !File.Exists(hostJsonPath))
            {
                string hostJsonString = @"{ }";
                File.WriteAllText(hostJsonPath, hostJsonString);
            }

            string taskAssemblyDirectory = Path.GetDirectoryName(typeof(GenerateFunctions).GetTypeInfo().Assembly.Location);
            string baseDirectory = Path.GetDirectoryName(taskAssemblyDirectory); 
            ProcessStartInfo processStartInfo = null;
#if NET46
            processStartInfo = GetProcessStartInfo(baseDirectory, isCore: false);
            if (UseNETCoreGenerator)
            {
                processStartInfo = GetProcessStartInfo(baseDirectory, isCore: true);
            }
#else
            processStartInfo = GetProcessStartInfo(baseDirectory, isCore: true);
            if (UseNETFrameworkGenerator)
            {
                processStartInfo = GetProcessStartInfo(baseDirectory, isCore: false);
            }

#endif
            this.Log.LogMessage(MessageImportance.Low, $"Function generator path: '{processStartInfo.FileName}'");
            this.Log.LogCommandLine(MessageImportance.Low, processStartInfo.Arguments);
            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output)) 
                { 
                    Log.LogWarning(output);
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    Log.LogError(error);
                    Log.LogError("Metadata generation failed.");
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private ProcessStartInfo GetProcessStartInfo(string baseLocation, bool isCore)
        {
            string workingDirectory = isCore ? Path.Combine(baseLocation, NETStandardFolder) : Path.Combine(baseLocation, NETFrameworkFolder);
            string exePath = isCore ? DotNetMuxer.MuxerPathOrDefault() : Path.Combine(workingDirectory, "Microsoft.NET.Sdk.Functions.Generator.exe");
            string arguments = isCore ? $"Microsoft.NET.Sdk.Functions.Generator.dll \"{TargetPath} \" \"{OutputPath} \"" : $"\"{TargetPath} \" \"{OutputPath} \"";

            string excludedFunctionNamesArg = UserProvidedFunctionJsonFiles?
                    .Select(f => f.ItemSpec.Replace("/", @"\").Replace(@"\function.json", string.Empty))
                    .Where(f => !f.Contains(@"\")) // only first level folders
                    .Aggregate((current, next) => $"{current};{next}");

            if (!string.IsNullOrEmpty(excludedFunctionNamesArg))
            {
                arguments += $" \"{excludedFunctionNamesArg}\"";
            }

            return new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory,
                FileName = exePath,
                Arguments = arguments 
            };
        }
    }
}
