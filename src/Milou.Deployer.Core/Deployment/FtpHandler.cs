using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpHandler
    {
        private const int DefaultBufferSize = 4096;

        private readonly Uri _ftpBaseUri;
        private readonly FtpSettings _ftpSettings;
        private readonly ILogger _logger;

        private readonly ICredentials _networkCredential;

        [PublicAPI]
        public FtpHandler([NotNull] Uri ftpBaseUri,
            [NotNull] ICredentials networkCredential,
            [NotNull] FtpSettings ftpSettings,
            ILogger logger = default)
        {
            _logger = logger ?? Logger.None;
            _ftpBaseUri = ftpBaseUri ??
                          throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(ftpBaseUri));
            _networkCredential = networkCredential ?? throw new ArgumentNullException(nameof(networkCredential));
            _ftpSettings = ftpSettings ?? throw new ArgumentNullException(nameof(ftpSettings));
        }

        private FtpRequest CreateRequest(FtpPath ftpPath, FtpMethod method)
        {
            var request = (FtpWebRequest)WebRequest.Create(
                $"{_ftpBaseUri.AbsoluteUri.TrimEnd('/')}/{ftpPath.Path.TrimStart('/').Replace("//", "/")}");
            request.Credentials = _networkCredential;
            request.EnableSsl = _ftpSettings.IsSecure;
            request.Method = method.Command;
            return new FtpRequest(request, method);
        }

        private Task<FtpResponse> CreateFtpResponseAsync(FtpPath filePath, FtpMethod method)
        {
            var request = CreateRequest(filePath, method);

            return GetFtpResponseAsync(request);
        }

        private static FtpPath ParseLine(string currentLine)
        {
            if (string.IsNullOrWhiteSpace(currentLine))
            {
                throw new FtpException("Current line is null or whitespace");
            }

            int metadataLength = 40;

            string metadata = currentLine.Substring(0, metadataLength);

            const string directoryType = "<DIR>";

            if (metadata.IndexOf(directoryType, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new FtpPath(
                    currentLine.Split(new[] {directoryType}, StringSplitOptions.RemoveEmptyEntries).Last().Trim(),
                    FileSystemType.Directory);
            }

            return new FtpPath(currentLine.Substring(metadataLength - 1), FileSystemType.File);
        }

        public Task<bool> DirectoryExistsAsync([NotNull] FtpPath dir, CancellationToken cancellationToken)
        {
            if (dir == null)
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

        private async Task<bool> DirectoryExistsInternalAsync(FtpPath dir, CancellationToken cancellationToken)
        {
            try
            {
                var listDir = dir;

                bool isRootPath = dir.IsRoot;

                if (!isRootPath)
                {
                    listDir = dir.Parent;
                }

                var items = await ListDirectoryAsync(listDir, false, cancellationToken);

                if (isRootPath)
                {
                    _logger.Debug("Successfully listed root directory '{Path}'", listDir.Path);
                    return true;
                }

                var foundPaths = items.Where(item => item.ContainsPath(dir)).ToArray();

                if (foundPaths.Length == 0)
                {
                    _logger.Debug("Could not find FTP Path {Path}", dir.Path);
                    return false;
                }
            }
            catch (WebException ex)
            {
                throw new FtpException($"Could not determine if directory '{dir.Path}'", ex);
            }

            return true;
        }

        private async Task<FtpResponse> GetFtpResponseAsync(FtpRequest request)
        {
            try
            {
                return new FtpResponse(await request.Request.GetResponseAsync());
            }
            catch (Exception ex)
            {
                throw new FtpException(
                    $"Could not make FTP request with method {request.Method} and path '{request.RequestUri.PathAndQuery}'",
                    ex);
            }
        }

        public Task UploadFileAsync(
            [NotNull] FtpPath filePath,
            [NotNull] FileInfo sourceFile,
            CancellationToken cancellationToken = default)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceFile == null)
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

        private async Task UploadFileInternalAsync(
            [NotNull] FtpPath filePath,
            [NotNull] FileInfo sourceFile,
            CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(filePath, FtpMethod.UploadFile);
            _logger.Debug("Settings file content upload length to {Length} bytes for file '{Path}'", sourceFile.Length,
                filePath.Path);
            request.Request.ContentLength = sourceFile.Length;

            try
            {
                using (var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open))
                {
                    using (var requestStream = await request.Request.GetRequestStreamAsync())
                    {
                        if (requestStream is null)
                        {
                            throw new FtpException($"FTP request stream is null for file path '{filePath.Path}'");
                        }

                        await sourceStream.CopyToAsync(requestStream, DefaultBufferSize, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FtpException(
                    $"Could not copy source file '{sourceFile.FullName}' to path '{filePath.Path}' stream", ex);
            }

            try
            {
                using (var response = await GetFtpResponseAsync(request))
                {
                    if (response.Response.StatusCode != FtpStatusCode.ClosingData)
                    {
                        throw new FtpException($"Could not upload file '{filePath.Path}'",
                            response.Response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FtpException($"Could not get response for FTP file upload to path '{filePath.Path}'", ex);
            }
        }

        public Task DeleteFileAsync([NotNull] FtpPath filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Type != FileSystemType.File)
            {
                throw new ArgumentException($"The ftp delete path '{filePath.Path}' is not a file");
            }

            return DeleteFileInternalAsync(filePath);
        }

        private async Task DeleteFileInternalAsync([NotNull] FtpPath filePath)
        {
            using (var response = await CreateFtpResponseAsync(filePath, FtpMethod.DeleteFile))
            {
                if (response.Response.StatusCode != FtpStatusCode.FileActionOK)
                {
                    throw new FtpException($"Could not delete file '{filePath}'");
                }
            }
        }

        public Task DeleteDirectoryAsync([NotNull] FtpPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Type != FileSystemType.Directory)
            {
                throw new ArgumentException($"The ftp delete path {path.Path} is not a directory");
            }

            return DeleteDirectoryInternalAsync(path);
        }

        private async Task DeleteDirectoryInternalAsync([NotNull] FtpPath path)
        {
            using (var response = await CreateFtpResponseAsync(path, FtpMethod.RemoveDirectory))
            {
                if (response.Response.StatusCode != FtpStatusCode.FileActionOK)
                {
                    throw new FtpException($"Could not delete directory '{path}'");
                }
            }
        }

        public Task FileExistsAsync([NotNull] FtpPath filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return FileExistsInternalAsync(filePath);
        }

        private async Task FileExistsInternalAsync([NotNull] FtpPath filePath)
        {
            using (var response = await CreateFtpResponseAsync(filePath, FtpMethod.GetFileSize))
            {
                if (response.Response.StatusCode != FtpStatusCode.CommandOK)
                {
                    throw new FtpException($"Could not delete directory '{filePath}'");
                }
            }
        }

        public Task CreateDirectoryAsync([NotNull] FtpPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (directoryPath.Type != FileSystemType.Directory)
            {
                throw new ArgumentException($"The ftp create path {directoryPath.Path} is not a directory");
            }

            return CreateDirectoryInternalAsync(directoryPath);
        }

        private async Task CreateDirectoryInternalAsync([NotNull] FtpPath directoryPath)
        {
            using (var response = await CreateFtpResponseAsync(directoryPath, FtpMethod.MakeDirectory))
            {
                if (response.Response.StatusCode != FtpStatusCode.PathnameCreated)
                {
                    throw new FtpException(
                        $"Could not created directory {directoryPath}, status {response.Response.StatusCode}");
                }
            }
        }

        public Task<ImmutableArray<FtpPath>> ListDirectoryAsync(
            [NotNull] FtpPath path,
            bool recursive = true,
            CancellationToken cancellationToken = default)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return ListDirectoryInternalAsync(path, recursive, cancellationToken);
        }

        private async Task<ImmutableArray<FtpPath>> ListDirectoryInternalAsync(
            [NotNull] FtpPath path,
            bool recursive,
            CancellationToken cancellationToken)
        {
            try
            {
                var paths = new List<FtpPath>();

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException(nameof(ListDirectoryAsync));
                }

                var directoryItems = await GetDirectoryItems(path, cancellationToken);

                foreach (string directoryItem in directoryItems)
                {
                    var directorySubItems = await AddDirectorItemsAsync(path, recursive, directoryItem, cancellationToken);

                    paths.AddRange(directorySubItems);
                }

                return paths.ToImmutableArray();
            }
            catch (WebException ex)
            {
                throw new FtpException($"Could not list files for directory '{path}'", ex);
            }
        }

        private async Task<List<FtpPath>> AddDirectorItemsAsync(FtpPath path,
            bool recursive,
            string directoryItem,
            CancellationToken cancellationToken)
        {
            var paths = new List<FtpPath>();

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException(nameof(ListDirectoryAsync));
            }

            var currentFtpPath = ParseLine(directoryItem);

            string currentFullPath = $"{path.Path.TrimEnd('/')}/{currentFtpPath.Path}";

            if (currentFtpPath.Type == FileSystemType.Directory)
            {
                paths.Add(new FtpPath(currentFullPath, FileSystemType.Directory));

                if (recursive)
                {
                    var immutableArrays = await ListDirectoryInternalAsync(
                        new FtpPath(currentFullPath, currentFtpPath.Type),
                        true,
                        cancellationToken);

                    paths.AddRange(immutableArrays);
                }
            }
            else
            {
                paths.Add(new FtpPath(currentFullPath, FileSystemType.File));
            }

            return paths;
        }

        private async Task<List<string>> GetDirectoryItems(FtpPath path, CancellationToken cancellationToken)
        {
            var currentLines = new List<string>();
            using (var response = await CreateFtpResponseAsync(path, FtpMethod.ListDirectoryDetails))
            {
                using (var reader = new StreamReader(response.Response.GetResponseStream() ??
                                                     throw new FtpException(
                                                         $"FTP response stream is null for path {path.Path}")))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null &&
                           !cancellationToken.IsCancellationRequested)
                    {
                        currentLines.Add(line);
                    }
                }
            }

            return currentLines;
        }

        public static FtpHandler CreateWithPublishSettings([NotNull] string publishSettingsFile,
            [NotNull] FtpSettings ftpSettings)
        {
            if (ftpSettings == null)
            {
                throw new ArgumentNullException(nameof(ftpSettings));
            }

            if (string.IsNullOrWhiteSpace(publishSettingsFile))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(publishSettingsFile));
            }

            var ftpPublishSettings = FtpPublishSettings.Load(publishSettingsFile);

            var credentials = new NetworkCredential(ftpPublishSettings.UserName, ftpPublishSettings.Password, "");

            var fullUri = ftpPublishSettings.FtpBaseUri;

            if (ftpSettings.BasePath != null)
            {
                var builder = new UriBuilder(fullUri) {Path = ftpSettings.BasePath.Path};

                fullUri = builder.Uri;
            }

            return new FtpHandler(fullUri, credentials, ftpSettings);
        }

        public Task<FtpSummary> UploadDirectoryAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            [NotNull] DirectoryInfo baseDirectory,
            CancellationToken cancellationToken)
        {
            if (ruleConfiguration == null)
            {
                throw new ArgumentNullException(nameof(ruleConfiguration));
            }

            if (sourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (baseDirectory == null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            return UploadDirectoryInternalAsync(ruleConfiguration, sourceDirectory, baseDirectory, cancellationToken);
        }

        private async Task<FtpSummary> UploadDirectoryInternalAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            [NotNull] DirectoryInfo baseDirectory,
            CancellationToken cancellationToken)
        {
            var dir = new FtpPath(PathHelper.RelativePath(sourceDirectory, baseDirectory),
                FileSystemType.Directory);

            var summary = new FtpSummary();

            bool directoryExists = await DirectoryExistsAsync(dir, cancellationToken);

            if (!directoryExists)
            {
                await CreateDirectoryAsync(dir);
                summary.CreatedDirectories.Add(dir.Path);
            }

            await UploadFilesAsync(sourceDirectory, baseDirectory, summary, cancellationToken);

            await UploadDirectoriesAsync(ruleConfiguration, sourceDirectory, baseDirectory, summary, cancellationToken);

            return summary;
        }

        private async Task UploadDirectoriesAsync(
            RuleConfiguration ruleConfiguration,
            DirectoryInfo sourceDirectory,
            DirectoryInfo baseDirectory,
            FtpSummary summary,
            CancellationToken cancellationToken)
        {
            foreach (var subDirectory in sourceDirectory.GetDirectories())
            {
                var summary1 =
                    await UploadDirectoryAsync(ruleConfiguration, subDirectory, baseDirectory, cancellationToken);
                summary.Add(summary1);
            }
        }

        private async Task UploadFilesAsync(
            DirectoryInfo sourceDirectory,
            DirectoryInfo baseDirectory,
            FtpSummary summary,
            CancellationToken cancellationToken)
        {
            foreach (var fileInfo in sourceDirectory.GetFiles())
            {
                var sourceFile =
                    new FtpPath(PathHelper.RelativePath(fileInfo, baseDirectory), FileSystemType.File);

                await UploadFileAsync(sourceFile, fileInfo, cancellationToken);

                summary.CreatedFiles.Add(sourceFile.Path);
            }
        }

        public Task<IDeploymentChangeSummary> PublishAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken)
        {
            if (ruleConfiguration == null)
            {
                throw new ArgumentNullException(nameof(ruleConfiguration));
            }

            if (sourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            return PublishInternalAsync(ruleConfiguration, sourceDirectory, cancellationToken);
        }

        private async Task<IDeploymentChangeSummary> PublishInternalAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken)
        {
            var deploymentChangeSummary = new FtpSummary();
            var fileSystemItems =
                await ListDirectoryAsync(FtpPath.Root, cancellationToken: cancellationToken);

            var excludedFiles =
                await DeleteFilesAsync(ruleConfiguration, fileSystemItems, deploymentChangeSummary);

            await DeleteDirectoriesAsync(ruleConfiguration, fileSystemItems, deploymentChangeSummary, excludedFiles);

            var uploadDirectoryAsync =
                await UploadDirectoryAsync(ruleConfiguration, sourceDirectory, sourceDirectory, cancellationToken);

            deploymentChangeSummary.Add(uploadDirectoryAsync);

            return deploymentChangeSummary;
        }

        private async Task DeleteDirectoriesAsync(RuleConfiguration ruleConfiguration,
            ImmutableArray<FtpPath> fileSystemItems,
            FtpSummary deploymentChangeSummary,
            List<FtpPath> excludedSegments)
        {
            foreach (var item in fileSystemItems
                .Where(fileSystemItem => fileSystemItem.Type == FileSystemType.Directory)
                .OrderByDescending(s => s.Path))
            {
                if (ruleConfiguration.AppDataSkipDirectiveEnabled && item.IsAppDataDirectoryOrFile)
                {
                    excludedSegments.Add(item);
                    continue;
                }

                if (ruleConfiguration.Excludes.Any(e => item.Path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
                {
                    excludedSegments.Add(item);
                    deploymentChangeSummary.IgnoredDirectories.Add(item.Path);
                    continue;
                }

                if (!excludedSegments.Any(excluded => excluded.ContainsPath(item)))
                {
                    await DeleteDirectoryAsync(item);

                    deploymentChangeSummary.DeletedDirectories.Add(item.Path);
                }
            }
        }

        private async Task<List<FtpPath>> DeleteFilesAsync(RuleConfiguration ruleConfiguration,
            ImmutableArray<FtpPath> fileSystemItems,
            FtpSummary deploymentChangeSummary)
        {
            var excludedSegments = new List<FtpPath>();

            foreach (var fileSystemItem in fileSystemItems
                .Where(fileSystemItem => fileSystemItem.Type == FileSystemType.File))
            {
                if (ruleConfiguration.AppDataSkipDirectiveEnabled && fileSystemItem.IsAppDataDirectoryOrFile)
                {
                    excludedSegments.Add(fileSystemItem);
                    continue;
                }

                if (ruleConfiguration.Excludes.Any(value =>
                    fileSystemItem.Path.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
                {
                    excludedSegments.Add(fileSystemItem);
                    deploymentChangeSummary.IgnoredFiles.Add(fileSystemItem.Path);
                    continue;
                }

                await DeleteFileAsync(fileSystemItem);

                deploymentChangeSummary.Deleted.Add(fileSystemItem.Path);
            }

            return excludedSegments;
        }
    }
}