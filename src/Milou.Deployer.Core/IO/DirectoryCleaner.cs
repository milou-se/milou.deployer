using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Milou.Deployer.Core.Extensions;
using Serilog;

namespace Milou.Deployer.Core.IO
{
    public sealed class DirectoryCleaner
    {
        private readonly ILogger _logger;

        public DirectoryCleaner([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void CleanFiles([NotNull] IEnumerable<string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            foreach (string file in files)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);

                        _logger.Verbose("Deleted clean file '{CleanFile}'", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Could not delete file '{FullName}'", file);
                }
            }
        }

        public void CleanDirectories(IEnumerable<DirectoryInfo> directoriesToClean)
        {
            if (directoriesToClean == null)
            {
                throw new ArgumentNullException(nameof(directoriesToClean));
            }

            foreach (
                DirectoryInfo tempDirectory in
                directoriesToClean.OrderByDescending(directory => directory.FullName.Length))
            {
                _logger.Debug("Deleting temp directory '{FullName}'", tempDirectory.FullName);

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

                    _logger.Warning("Could not delete directory or any of it's sub paths '{FullName}', {Message}",
                        tempDirectory.FullName,
                        message);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Warning("Could not delete directory or any of it's sub paths '{FullName}', {Ex}",
                        tempDirectory.FullName,
                        ex);
                }
            }
        }
    }
}
