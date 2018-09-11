using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Serilog;

namespace Milou.Deployer.Core.IO
{
    public static class RecursiveIO
    {
        private static readonly ImmutableHashSet<string> _DeniedExtensions =
            new HashSet<string> { ".user", ".ncrunchproject", ".dotsettings", ".csproj" }
                .ToImmutableHashSet();

        public static void RecursiveCopy(
            DirectoryInfo sourceDirectoryInfo,
            DirectoryInfo targetDirectoryInfo,
            [NotNull] ILogger logger,
            ImmutableArray<string> excludedFilePatterns)
        {
            if (sourceDirectoryInfo == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectoryInfo));
            }

            if (targetDirectoryInfo == null)
            {
                throw new ArgumentNullException(nameof(targetDirectoryInfo));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            sourceDirectoryInfo.Refresh();
            targetDirectoryInfo.Refresh();

            if (!targetDirectoryInfo.Exists)
            {
                logger.Verbose("Creating directory '{FullName}'", targetDirectoryInfo.FullName);
                targetDirectoryInfo.Create();
            }

            ImmutableArray<FileInfo> deniedFilesInDirectory = excludedFilePatterns
                .Select(sourceDirectoryInfo.GetFiles).SelectMany(files => files)
                .ToImmutableArray();

            foreach (FileInfo currentFile in sourceDirectoryInfo.GetFiles())
            {
                if (_DeniedExtensions.Any(denied =>
                    currentFile.Extension.Length > 0
                    && denied.Equals(currentFile.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.Verbose("Skipping denied file '{FullName}' due to its file extension",
                        currentFile.FullName);
                    continue;
                }

                if (deniedFilesInDirectory.Any(file =>
                    file.FullName.Equals(currentFile.FullName, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.Verbose("Skipping denied file '{FullName}' due to its file pattern {Patterns}",
                        currentFile.FullName,
                        excludedFilePatterns);
                    continue;
                }

                var targetFile = new FileInfo(Path.Combine(targetDirectoryInfo.FullName, currentFile.Name));
                logger.Verbose("Copying file '{FullName}' to '{FullName1}'", currentFile.FullName, targetFile.FullName);
                currentFile.CopyTo(targetFile.FullName, true);
            }

            foreach (DirectoryInfo subDirectory in sourceDirectoryInfo.GetDirectories())
            {
                var targetSubDirectory = new DirectoryInfo(
                    Path.Combine(targetDirectoryInfo.FullName, subDirectory.Name));

                RecursiveCopy(subDirectory, targetSubDirectory, logger, excludedFilePatterns);
            }
        }

        public static void RecursiveDelete(DirectoryInfo sourceDirectoryInfo, [NotNull] ILogger logger)
        {
            if (sourceDirectoryInfo == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectoryInfo));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            sourceDirectoryInfo.Refresh();

            if (!sourceDirectoryInfo.Exists)
            {
                logger.Verbose("Cannot delete directory '{FullName}', it does not exist", sourceDirectoryInfo.FullName);
                return;
            }

            foreach (DirectoryInfo subDirectory in sourceDirectoryInfo.GetDirectories())
            {
                RecursiveDelete(subDirectory, logger);
            }

            foreach (FileInfo fileInfo in sourceDirectoryInfo.GetFiles())
            {
                logger.Verbose("Deleting file '{FullName}'", fileInfo.FullName);
                fileInfo.Delete();
                logger.Verbose("Deleted file '{FullName}'", fileInfo.FullName);
            }

            logger.Verbose("Deleting directory '{FullName}'", sourceDirectoryInfo.FullName);
            sourceDirectoryInfo.Delete(true);
            logger.Verbose("Deleted directory '{FullName}'", sourceDirectoryInfo.FullName);
        }
    }
}
