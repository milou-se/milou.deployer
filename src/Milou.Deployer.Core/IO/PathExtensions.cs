using System;
using System.IO;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.IO
{
    public static class PathExtensions
    {
        public static string NormalizePath([NotNull] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            return path.Trim(Path.DirectorySeparatorChar);
        }

        public static string GetRelativePath([NotNull] this FileInfo file, [NotNull] DirectoryInfo rootPath)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            string rootFullPath = NormalizePath(rootPath.FullName);
            string fullPath = NormalizePath(file.FullName);

            return GetRelativePath(fullPath, rootFullPath);
        }

        public static string GetRelativePath([NotNull] this DirectoryInfo directoryInfo, [NotNull] DirectoryInfo rootPath)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            string rootFullPath = NormalizePath(rootPath.FullName);
            string fullPath = NormalizePath(directoryInfo.FullName);

            return GetRelativePath(fullPath, rootFullPath);
        }

        private static string GetRelativePath(this string fullPath, string rootFullPath)
        {
            if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Could not find rootPath in fullPath when calculating relative path.");
            }

            return fullPath.Substring(rootFullPath.Length);
        }
    }
}
