using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.Functions.MSBuild.Tasks
{
    public class DeleteFileTask : Task
    {
        [Required]
        public string FilePath { get; set; }

        public override bool Execute()
        {
            if(!File.Exists(FilePath))
            {
                return false;
            }

            File.Delete(FilePath);
            return true;
        }
    }
}
