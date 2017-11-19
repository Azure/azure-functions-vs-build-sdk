using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MakeFunctionJson
{
    internal class PathUtility
    {
        internal static string MakeRelativePath(string fromPath, string toPath)
        {
            fromPath = fromPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            // This will generate a path using / rather than \.
            // This is okay on both Windows and Unix
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }
    }
}