using System.Diagnostics;
using System.IO;
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
        private const string NETStandardFolder = "netstandard2.0";

        public bool UseNETCoreGenerator { get; set; }

        public bool UseNETFrameworkGenerator { get; set; }

        public bool GenerateHostJson { get; set; }

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
            string arguments = isCore ? $"Microsoft.NET.Sdk.Functions.Generator.dll \"{TargetPath}\" \"{OutputPath}\"" : $"\"{TargetPath}\" \"{OutputPath}\"";
            return new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory,
                FileName = exePath,
                Arguments = arguments 
            };
        }
    }
}