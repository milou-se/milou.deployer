﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using FluentFTP;
using JetBrains.Annotations;
using Milou.Deployer.Core;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;

using Milou.Deployer.Core.IO;
using Serilog;
using Serilog.Core;
using FtpException = Milou.Deployer.Core.Deployment.Ftp.FtpException;

namespace Milou.Deployer.Ftp
{
    public sealed class FtpHandler : IFtpHandler
    {
        private readonly FtpClient _ftpClient;
        private readonly FtpSettings _ftpSettings;
        private readonly ILogger _logger;

        [PublicAPI]
        public FtpHandler(
            [NotNull] FtpClient ftpClient,
            [CanBeNull] ILogger? logger = default,
            FtpSettings? ftpSettings = null)
        {
            _ftpSettings = ftpSettings ?? new FtpSettings();
            _logger = logger ?? Logger.None;
            _ftpClient = ftpClient ?? throw new ArgumentNullException(nameof(ftpClient));
            _ftpClient.OnLogEvent += FtpClientOnLogEvent;
        }

        public Task<bool> DirectoryExistsAsync(FtpPath dir, CancellationToken cancellationToken)
        {
            if (dir is null)
            {
                throw new ArgumentNullException(nameof(dir));
            }

            if (dir.Type != FileSystemType.Directory)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, Resources.FtpFtpPathMustBeADirectoryPath, dir.Path),
                    nameof(dir));
            }

            return DirectoryExistsInternalAsync(dir, cancellationToken);
        }

        public Task UploadFileAsync(
            FtpPath filePath,
            FileInfo sourceFile,
            CancellationToken cancellationToken = default)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceFile is null)
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (filePath.Type != FileSystemType.File)
            {
                throw new ArgumentException($"The ftp upload path '{filePath.Path}' is not a file");
            }

            if (!sourceFile.Exists)
            {
                throw new FtpException($"Source file '{sourceFile.FullName}' does not exist");
            }

            return UploadFileInternalAsync(filePath, sourceFile, cancellationToken);
        }

        public Task DeleteFileAsync(FtpPath filePath, CancellationToken cancellationToken)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Type != FileSystemType.File)
            {
                throw new ArgumentException($"The ftp delete path '{filePath.Path}' is not a file");
            }

            return DeleteFileInternalAsync(filePath, cancellationToken);
        }

        public Task DeleteDirectoryAsync(FtpPath path, CancellationToken cancellationToken)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Type != FileSystemType.Directory)
            {
                throw new ArgumentException($"The ftp delete path {path.Path} is not a directory");
            }

            return DeleteDirectoryInternalAsync(path, cancellationToken);
        }

        public Task<bool> FileExistsAsync(FtpPath filePath, CancellationToken cancellationToken)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return FileExistsInternalAsync(filePath, cancellationToken);
        }

        public Task CreateDirectoryAsync(FtpPath directoryPath, CancellationToken cancellationToken)
        {
            if (directoryPath is null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (directoryPath.Type != FileSystemType.Directory)
            {
                throw new ArgumentException($"The ftp create path {directoryPath.Path} is not a directory");
            }

            return CreateDirectoryInternalAsync(directoryPath, cancellationToken);
        }

        public Task<ImmutableArray<FtpPath>> ListDirectoryAsync(
            FtpPath path,
            CancellationToken cancellationToken = default)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return ListDirectoryInternalAsync(path, cancellationToken);
        }

        public Task<DeploySummary> UploadDirectoryAsync(
            RuleConfiguration ruleConfiguration,
            DirectoryInfo sourceDirectory,
            DirectoryInfo baseDirectory,
            FtpPath basePath,
            CancellationToken cancellationToken)
        {
            if (ruleConfiguration is null)
            {
                throw new ArgumentNullException(nameof(ruleConfiguration));
            }

            if (sourceDirectory is null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            if (basePath is null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            return UploadDirectoryInternalAsync(sourceDirectory, baseDirectory, basePath, cancellationToken);
        }

        public Task<DeploySummary> PublishAsync(
            RuleConfiguration ruleConfiguration,
            DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken)
        {
            if (ruleConfiguration is null)
            {
                throw new ArgumentNullException(nameof(ruleConfiguration));
            }

            if (sourceDirectory is null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            return PublishInternalAsync(ruleConfiguration, sourceDirectory, cancellationToken);
        }

        public void Dispose() => _ftpClient?.Dispose();

        private async Task<bool> DirectoryExistsInternalAsync(FtpPath dir, CancellationToken cancellationToken)
        {
            try
            {
                bool directoryExists = await _ftpClient.DirectoryExistsAsync(dir.Path, cancellationToken);

                _logger.Verbose("Directory {Directory} exists: {Exists}", dir, directoryExists);

                return directoryExists;
            }
            catch (Exception ex)
            {
                throw new FtpException($"Could not determine if directory '{dir.Path}' exists", ex);
            }
        }

        private async Task UploadFileInternalAsync(
            [NotNull] FtpPath filePath,
            [NotNull] FileInfo sourceFile,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Settings file content upload length to {Length} bytes for file '{Path}'",
                sourceFile.Length,
                filePath.Path);

            IProgress<FtpProgress> progress = GetProgressAction();

            try
            {
                await _ftpClient.UploadFileAsync(sourceFile.FullName,
                    filePath.Path,
                    FtpRemoteExists.Overwrite,
                    true,
                    FtpVerify.Delete | FtpVerify.Retry,
                    progress,
                    cancellationToken);

                _logger.Verbose("Uploaded {SourceFile} to {TargetFile}", sourceFile.FullName, filePath.Path);
            }
            catch (Exception ex)
            {
                throw new FtpException(
                    $"Could not copy source file '{sourceFile.FullName}' to path '{filePath.Path}' stream",
                    ex);
            }
        }

        private IProgress<FtpProgress> GetProgressAction()
        {
            IProgress<FtpProgress> progress = new Progress<FtpProgress>(p =>
                _logger.Information("Progress {Percent}", p.Progress.ToString("F0", CultureInfo.InvariantCulture)));

            return progress;
        }

        private async Task DeleteFileInternalAsync([NotNull] FtpPath filePath, CancellationToken cancellationToken)
        {
            int attempt = 1;
            const int maxAttempts = 5;

            while (true)
            {
                try
                {
                    await _ftpClient.DeleteFileAsync(filePath.Path, cancellationToken);

                    _logger.Verbose("Delete file {FilePath}", filePath.Path);

                    return;
                }
                catch (Exception ex)
                {
                    if (attempt > maxAttempts)
                    {
                        throw new FtpException($"Could not delete file '{filePath.Path}'", ex);
                    }

                    if (!await _ftpClient.FileExistsAsync(filePath.Path, cancellationToken))
                    {
                        _logger.Verbose(ex, "Could not delete file because it does not exists");
                        return;
                    }

                    attempt++;
                    _logger.Verbose(ex, "FPT Error, retrying");
                    await Task.Delay(TimeSpan.FromMilliseconds(attempt * 50), cancellationToken);
                }
            }
        }

        private async Task DeleteDirectoryInternalAsync([NotNull] FtpPath path, CancellationToken cancellationToken)
        {
            try
            {
                await _ftpClient.DeleteDirectoryAsync(path.Path, FtpListOption.Recursive, cancellationToken);

                _logger.Verbose("Deleted directory {Path}", path.Path);
            }
            catch (Exception ex)
            {
                throw new FtpException($"Could not delete directory '{path.Path}'", ex);
            }
        }

        private async Task<bool> FileExistsInternalAsync(
            [NotNull] FtpPath filePath,
            CancellationToken cancellationToken)
        {
            try
            {
                bool exists = await _ftpClient.FileExistsAsync(filePath.Path, cancellationToken);

                _logger.Verbose("File {File} exists: {Exists}", filePath.Path, exists);

                return exists;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                throw new FtpException($"Could not determine if file '{filePath.Path}' exists", ex);
            }
        }

        private async Task CreateDirectoryInternalAsync(
            [NotNull] FtpPath directoryPath,
            CancellationToken cancellationToken)
        {
            try
            {
                await _ftpClient.CreateDirectoryAsync(directoryPath.Path, cancellationToken);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                throw new FtpException($"Could not created directory {directoryPath}", ex);
            }
        }

        private async Task<ImmutableArray<FtpPath>> ListDirectoryInternalAsync(
            [NotNull] FtpPath path,
            CancellationToken cancellationToken)
        {
            try
            {
                FtpListItem[] ftpListItems =
                    await _ftpClient.GetListingAsync(path.Path,
                        FtpListOption.AllFiles | FtpListOption.Recursive,
                        cancellationToken);

                return ftpListItems.Select(s => new FtpPath(s.FullName,
                        s.Type == FtpFileSystemObjectType.File
                            ? FileSystemType.File
                            : FileSystemType.Directory))
                    .ToImmutableArray();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                throw new FtpException($"Could not list files for directory '{path}'", ex);
            }
        }

        private async Task<DeploySummary> UploadDirectoryInternalAsync(
            [NotNull] DirectoryInfo sourceDirectory,
            [NotNull] DirectoryInfo baseDirectory,
            FtpPath basePath,
            CancellationToken cancellationToken)
        {
            FtpPath dir = basePath.Append(new FtpPath(PathHelper.RelativePath(sourceDirectory, baseDirectory),
                FileSystemType.Directory));

            var summary = new DeploySummary();

            bool directoryExists = await DirectoryExistsAsync(dir, cancellationToken);

            if (!directoryExists)
            {
                await CreateDirectoryAsync(dir, cancellationToken);
                summary.CreatedDirectories.Add(dir.Path);
            }

            DeploySummary uploadSummary =
                await UploadFilesAsync(sourceDirectory, baseDirectory, basePath, cancellationToken);

            summary.Add(uploadSummary);

            return summary;
        }

        private async Task<DeploySummary> UploadFilesAsync(
            DirectoryInfo sourceDirectory,
            DirectoryInfo baseDirectory,
            FtpPath basePath,
            CancellationToken cancellationToken)
        {
            var summary = new DeploySummary();

            string[] localPaths = sourceDirectory
                .GetFiles()
                .Select(f => f.FullName)
                .ToArray();

            int totalCount = localPaths.Length;

            if (totalCount == 0)
            {
                return summary;
            }

            FtpPath relativeDir = basePath.Append(new FtpPath(PathHelper.RelativePath(sourceDirectory, baseDirectory),
                FileSystemType.Directory));

            _logger.Verbose("Uploading {Files} files", localPaths.Length);

            int batchSize = _ftpSettings.BatchSize;

            var totalTime = Stopwatch.StartNew();

            try
            {
                int batches = (int)Math.Ceiling(totalCount / (double)batchSize);

                int uploaded = 0;

                for (int i = 0; i < batches; i++)
                {
                    bool batchSuccessful = false;

                    int batchNumber = i + 1;

                    string[] files = localPaths.Skip(i * batchSize).Take(batchSize).ToArray();

                    var stopwatch = new Stopwatch();

                    for (int j = 0; j < _ftpSettings.MaxAttempts; j++)
                    {
                        try
                        {
                            stopwatch.Restart();

                            int uploadedFiles = await _ftpClient.UploadFilesAsync(files,
                                relativeDir.Path,
                                FtpRemoteExists.Overwrite,
                                true,
                                FtpVerify.Delete | FtpVerify.Retry,
                                token: cancellationToken);

                            if (uploadedFiles == files.Length)
                            {
                                batchSuccessful = true;
                                break;
                            }

                            _logger.Warning(
                                "The expected number of uploaded files was {Expected} but result was {Actual}, retrying batch {Batch}",
                                files.Length,
                                uploadedFiles,
                                batchNumber);
                        }
                        catch (Exception ex) when (!ex.IsFatal())
                        {
                            _logger.Warning(ex, "FTP ERROR in batch {Batch}", batchNumber);
                        }
                        finally
                        {
                            stopwatch.Stop();
                        }
                    }

                    if (!batchSuccessful)
                    {
                        string message = batches > 1
                            ? $"The batch {batchNumber} failed"
                            : $"Failed to upload files {files}";

                        throw new InvalidOperationException(message);
                    }

                    uploaded += files.Length;
                    string elapsed = $"{stopwatch.Elapsed.TotalSeconds:F2}";

                    string percentage = $"{(100.0D * uploaded) / totalCount:F1}";

                    string paddedPercentage = new string(' ', 5 - percentage.Length) + percentage;

                    double averageTime = totalTime.Elapsed.TotalSeconds / uploaded;

                    string average = averageTime.ToString("F2", CultureInfo.InvariantCulture);

                    string timeLeft = $"{(totalCount - uploaded) * averageTime:F2}s";

                    string paddedBatch =
                        $"{new string(' ', batches.ToString(CultureInfo.InvariantCulture).Length - batchNumber.ToString(CultureInfo.InvariantCulture).Length)}{batchNumber}";

                    string paddedUploaded =
                        $"{new string(' ', totalCount.ToString(CultureInfo.InvariantCulture).Length - uploaded.ToString(CultureInfo.InvariantCulture).Length)}{uploaded}";

                    string totalElapsed = totalTime.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture);

                    if (batches > 1)
                    {
                        _logger.Information(
                            "Uploaded batch {BatchNumber} of {BatchCount} using batch size {Size}, {Uploaded}/{Total} {Percentage}%, average {Average}s per file, time left: ~{TimeLeft}, took {ElapsedTime}s, total time {TotalTime}s",
                            paddedBatch,
                            batches,
                            batchSize,
                            paddedUploaded,
                            totalCount,
                            paddedPercentage,
                            average,
                            timeLeft,
                            elapsed,
                            totalElapsed);
                    }
                    else
                    {
                        _logger.Information("Uploaded files {Files}, took {TotalElapsed}s", files, totalElapsed);
                    }
                }
            }
            finally
            {
                totalTime.Stop();
            }

            foreach (string path in localPaths)
            {
                summary.CreatedFiles.Add(PathHelper.RelativePath(new FileInfo(path), baseDirectory));
            }

            foreach (DirectoryInfo directoryInfo in sourceDirectory.GetDirectories())
            {
                DeploySummary subSummary =
                    await UploadFilesAsync(directoryInfo, baseDirectory, basePath, cancellationToken);

                summary.Add(subSummary);
            }

            return summary;
        }

        private async Task<DeploySummary> PublishInternalAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken)
        {
            var deploymentChangeSummary = new DeploySummary();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                FtpPath basePath = _ftpSettings.BasePath ?? FtpPath.Root;

                if (!await DirectoryExistsAsync(basePath, cancellationToken))
                {
                    await CreateDirectoryAsync(basePath, cancellationToken);
                }

                _logger.Debug("Listing files in remote path '{Path}'", basePath.Path);

                var fileSystemItems =
                    await ListDirectoryAsync(basePath, cancellationToken);

                FtpPath[] sourceFiles = sourceDirectory
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Select(s =>
                        basePath.Append(new FtpPath(PathHelper.RelativePath(s, sourceDirectory), FileSystemType.File)))
                    .ToArray();

                FtpPath[] filesToKeep = fileSystemItems
                    .Where(s => KeepFile(s, ruleConfiguration))
                    .Where(s => s.Type == FileSystemType.File)
                    .ToArray();

                var filesToRemove = fileSystemItems
                    .Except(sourceFiles)
                    .Except(filesToKeep)
                    .Where(s => s.Type == FileSystemType.File)
                    .ToImmutableArray();

                IEnumerable<FtpPath> updated = fileSystemItems.Except(filesToRemove)
                    .Where(s => s.Type == FileSystemType.File);

                if (ruleConfiguration.AppOfflineEnabled)
                {
                    using var tempFile = TempFile.CreateTempFile("App_Offline", ".htm");
                    var appOfflinePath = new FtpPath($"/{tempFile.File.Name}", FileSystemType.File);

                    FtpPath appOfflineFullPath =
                        (_ftpSettings.PublicRootPath ?? _ftpSettings.BasePath ?? FtpPath.Root).Append(
                            appOfflinePath);

                    await UploadFileAsync(appOfflineFullPath, tempFile.File, cancellationToken);

                    _logger.Debug("Uploaded file '{App_Offline}'", appOfflineFullPath.Path);
                }

                DeploySummary deleteFiles = await DeleteFilesAsync(ruleConfiguration, filesToRemove, cancellationToken);

                deploymentChangeSummary.Add(deleteFiles);

                foreach (FtpPath ftpPath in filesToRemove)
                {
                    deploymentChangeSummary.DeletedFiles.Add(ftpPath.Path);
                }

                foreach (FtpPath ftpPath in updated)
                {
                    deploymentChangeSummary.UpdatedFiles.Add(ftpPath.Path);
                }

                DeploySummary uploadDirectoryAsync =
                    await UploadDirectoryAsync(ruleConfiguration,
                        sourceDirectory,
                        sourceDirectory,
                        basePath,
                        cancellationToken);

                deploymentChangeSummary.Add(uploadDirectoryAsync);

                if (ruleConfiguration.AppOfflineEnabled)
                {
                    FtpPath[] appOfflineFiles = sourceFiles.Intersect(fileSystemItems)
                        .Where(file => file.Path is {} &&
                                       Path.GetFileName(file.Path)
                                           .Equals(DeploymentConstants.AppOfflineHtm,
                                               StringComparison.OrdinalIgnoreCase))
                        .Select(file => file.Path)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(file => new FtpPath(file, FileSystemType.File))
                        .ToArray();

                    foreach (FtpPath appOfflineFile in appOfflineFiles)
                    {
                        bool fileExists = await FileExistsAsync(appOfflineFile, cancellationToken);

                        if (fileExists)
                        {
                            await DeleteFileAsync(appOfflineFile, cancellationToken);

                            _logger.Debug("Deleted {App_Offline}", appOfflineFile.Path);
                        }
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
            }

            deploymentChangeSummary.TotalTime = stopwatch.Elapsed;

            return deploymentChangeSummary;
        }

        private async Task<DeploySummary> DeleteFilesAsync(
            RuleConfiguration ruleConfiguration,
            ImmutableArray<FtpPath> fileSystemItems,
            CancellationToken cancellationToken)
        {
            var deploymentChangeSummary = new DeploySummary();

            foreach (FtpPath fileSystemItem in fileSystemItems
                .Where(fileSystemItem => fileSystemItem.Type == FileSystemType.File))
            {
                if (ruleConfiguration.AppDataSkipDirectiveEnabled && fileSystemItem.IsAppDataDirectoryOrFile)
                {
                    continue;
                }

                if (ruleConfiguration.Excludes.Any(value =>
                    fileSystemItem.Path.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                {
                    deploymentChangeSummary.IgnoredFiles.Add(fileSystemItem.Path);
                    continue;
                }

                await DeleteFileAsync(fileSystemItem, cancellationToken);

                deploymentChangeSummary.DeletedFiles.Add(fileSystemItem.Path);
            }

            return deploymentChangeSummary;
        }

        private static bool KeepFile(FtpPath fileSystemItem, RuleConfiguration ruleConfiguration)
        {
            if (ruleConfiguration.AppDataSkipDirectiveEnabled && fileSystemItem.IsAppDataDirectoryOrFile)
            {
                return true;
            }

            if (ruleConfiguration.Excludes.Any(value =>
                fileSystemItem.Path.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private void FtpClientOnLogEvent(FtpTraceLevel level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            int indexOf = message.IndexOf("at System.Net.Sockets.Socket", StringComparison.Ordinal);

            if (indexOf >= 0)
            {
                message = message.Substring(0, indexOf).Trim();
            }

            const string messageTemplate = "{FtpMessage}";

            switch (level)
            {
                case FtpTraceLevel.Info:
                    _logger.Debug(messageTemplate, message);
                    break;
                case FtpTraceLevel.Error:
                    _logger.Warning(messageTemplate, message);
                    break;
                case FtpTraceLevel.Verbose:
                    _logger.Debug(messageTemplate, message);
                    break;
                case FtpTraceLevel.Warn:
                    _logger.Warning(messageTemplate, message);
                    break;
            }
        }

        public static async Task<FtpHandler> CreateWithPublishSettings(
            [NotNull] string publishSettingsFile,
            [NotNull] FtpSettings ftpSettings,
            ILogger? logger = default)
        {
            if (ftpSettings is null)
            {
                throw new ArgumentNullException(nameof(ftpSettings));
            }

            logger ??= Logger.None;

            if (string.IsNullOrWhiteSpace(publishSettingsFile))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(publishSettingsFile));
            }

            var ftpPublishSettings = FtpPublishSettings.Load(publishSettingsFile);

            var credentials = new NetworkCredential(ftpPublishSettings.UserName, ftpPublishSettings.Password, "");

            Uri fullUri = ftpPublishSettings.FtpBaseUri;

            if (ftpSettings.BasePath is {})
            {
                var builder = new UriBuilder(fullUri) {Path = ftpSettings.BasePath.Path};

                fullUri = builder.Uri;
            }

            return await Create(fullUri, ftpSettings, credentials, logger);
        }

        public static async Task<FtpHandler> Create(Uri fullUri, FtpSettings? ftpSettings = null, NetworkCredential? credentials = null,
            ILogger? logger = default)
        {
            logger ??= Logger.None;

            var ftpClient = new FtpClient(fullUri.Host, credentials)
            {
                SocketPollInterval = 1000,
                ConnectTimeout = 2000,
                ReadTimeout = 5000,
                DataConnectionConnectTimeout = 2000,
                DataConnectionReadTimeout = 2000,
                DataConnectionType = FtpDataConnectionType.PASV,
                Port = fullUri.Port
            };

            if (ftpSettings?.IsSecure ?? false)
            {
                logger.Debug("Using secure FTP connection");
                ftpClient.EncryptionMode = FtpEncryptionMode.Explicit;
                ftpClient.SslProtocols = SslProtocols.Tls12;
            }

            try
            {
                logger.Debug("Connecting to FTP");
                await ftpClient.ConnectAsync();
                logger.Debug("Connected to FTP");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                throw new FtpException($"Could not connect to FTP {fullUri.Host}", ex);
            }

            return new FtpHandler(ftpClient, logger, ftpSettings);
        }
    }
}