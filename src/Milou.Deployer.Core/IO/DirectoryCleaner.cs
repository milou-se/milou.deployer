using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;

using Serilog;

namespace Milou.Deployer.Core.IO
{
    public sealed class DirectoryCleaner
    {
        private readonly ILogger _logger;

        public DirectoryCleaner([NotNull] ILogger logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task CleanFilesAsync([NotNull] IList<string> files, int attempt = 0)
        {
            if (files is null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            if (attempt == 5)
            {
                return;
            }

            if (files.Count == 0)
            {
                return;
            }

            string[] toDelete = files.ToArray();

            foreach (string file in toDelete)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        _logger.Verbose("Deleting clean file '{CleanFile}'", file);

                        File.Delete(file);
                        _logger.Verbose("Deleted clean file '{CleanFile}'", file);
                    }

                    files.Remove(file);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Warning(ex, "Could not delete file '{FullName}'", file);
                }
            }

            if (files.Count > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                await CleanFilesAsync(files, attempt + 1);
            }
        }

        public async Task CleanDirectoriesAsync(
            IList<DirectoryInfo> directoriesToClean,
            int attempt = 0)
        {
            if (directoriesToClean is null)
            {
                throw new ArgumentNullException(nameof(directoriesToClean));
            }

            if (attempt == 5)
            {
                return;
            }

            if (directoriesToClean.Count == 0)
            {
                return;
            }

            foreach (DirectoryInfo tempDirectory in
                directoriesToClean.OrderByDescending(directory => directory.FullName.Length))
            {
                _logger.Verbose("Deleting temp directory '{FullName}'", tempDirectory.FullName);

                try
                {
                    RecursiveIO.RecursiveDelete(tempDirectory, _logger);
                }
                catch (AggregateException ex)
                {
                    var message = new StringBuilder();

                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        message.AppendLine(innerException.ToString());
                    }

                    _logger.Warning(
                        "Could not delete directory or any of it's sub paths '{FullName}', {Message}",
                        tempDirectory.FullName,
                        message);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Warning(
                        ex,
                        "Could not delete directory or any of it's sub paths '{FullName}'",
                        tempDirectory.FullName);
                }
            }

            foreach (DirectoryInfo directoryInfo in directoriesToClean)
            {
                directoryInfo.Refresh();
            }

            var nonEmptyDirectories = directoriesToClean
                .Where(dir => dir.Exists && (dir.GetFiles().Length > 0 || dir.GetDirectories().Length > 0))
                .ToList();

            if (nonEmptyDirectories.Count > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                await CleanDirectoriesAsync(nonEmptyDirectories, attempt + 1);
            }
        }
    }
}