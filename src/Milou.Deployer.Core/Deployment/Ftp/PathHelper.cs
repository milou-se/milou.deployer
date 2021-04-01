﻿using System;
using System.IO;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public static class PathHelper
    {
        public static string RelativePath([NotNull] FileInfo fileInfo, [NotNull] DirectoryInfo baseDirectory)
        {
            if (fileInfo is null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            string fullName = fileInfo.FullName;

            string baseFullName = baseDirectory.FullName;

            return GetRelative(fullName, baseFullName);
        }

        public static string RelativePath([NotNull] DirectoryInfo directoryInfo, [NotNull] DirectoryInfo baseDirectory)
        {
            if (directoryInfo is null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            string fullName = directoryInfo.FullName;

            string baseFullName = baseDirectory.FullName;

            return GetRelative(fullName, baseFullName);
        }

        private static string GetRelative(string fullName, string baseFullName)
        {
            if (!fullName.StartsWith(baseFullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The file path '{fullName}' does not start with '{baseFullName}'");
            }

            string substring = fullName[baseFullName.Length..];

            string relative = $"/{substring.Replace("\\", "/", StringComparison.Ordinal).TrimStart('/')}";

            return relative;
        }
    }
}