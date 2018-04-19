using System;
using System.IO;

namespace Milou.Deployer.Core.Deployment
{
    public class FileMatch
    {
        public FileMatch(string targetName, FileInfo actionFile, DirectoryInfo actionFileRootDirectory)
        {
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            ActionFile = actionFile ?? throw new ArgumentNullException(nameof(actionFile));
            ActionFileRootDirectory = actionFileRootDirectory ?? throw new ArgumentNullException(nameof(actionFileRootDirectory));
        }

        public string TargetName { get; }

        public FileInfo ActionFile { get; }

        public DirectoryInfo ActionFileRootDirectory { get; }
    }
}
