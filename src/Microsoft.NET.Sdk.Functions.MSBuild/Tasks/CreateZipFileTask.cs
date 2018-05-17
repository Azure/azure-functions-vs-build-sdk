using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
    public class CreateZipFileTask : Task
    {
        [Required]
        public string FolderToZip { get; set; }

        [Output]
        public string CreatedZipPath { get; private set; }

        public override bool Execute()
        {
            string zipFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".zip";
            CreatedZipPath = Path.Combine(Path.GetTempPath(), zipFileName);
            ZipFile.CreateFromDirectory(FolderToZip, CreatedZipPath);
            return true;
        }
    }
}
