using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpHandler
    {
        private const int DefaultBufferSize = 4096;

        private readonly Uri _ftpBaseUri;

        private readonly ICredentials _networkCredential;

        public FtpHandler([NotNull] Uri ftpBaseUri, [NotNull] ICredentials networkCredential)
        {
            _ftpBaseUri = ftpBaseUri ??
                          throw new ArgumentException("Value cannot be null or whitespace.", nameof(ftpBaseUri));
            _networkCredential = networkCredential ?? throw new ArgumentNullException(nameof(networkCredential));
        }

        private FtpRequest CreateRequest(FtpPath ftpPath, FtpMethod method)
        {
            var request = (FtpWebRequest) WebRequest.Create(
                $"{_ftpBaseUri.AbsoluteUri.TrimEnd('/')}/{ftpPath.Path.TrimStart('/').Replace("//", "/")}");
            request.Credentials = _networkCredential;
            request.EnableSsl = true;
            request.Method = method.Command;
            return new FtpRequest(request, method);
        }

        private Task<FtpResponse> CreateFtpResponseAsync(FtpPath filePath, FtpMethod method)
        {
            FtpRequest request = CreateRequest(filePath, method);

            return GetFtpResponseAsync(request);
        }

        private static FtpPath ParseLine(string currentLine)
        {
            int metadataLength = 40;

            string metadata = currentLine.Substring(0, metadataLength);

            const string directoryType = "<DIR>";

            if (metadata.IndexOf(directoryType, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new FtpPath(
                    currentLine.Split(new[] { directoryType }, StringSplitOptions.RemoveEmptyEntries).Last().Trim(),
                    FileSystemType.Directory);
            }

            return new FtpPath(currentLine.Substring(metadataLength - 1), FileSystemType.File);
        }

        private async Task<bool> DirectoryExistsAsync(FtpPath dir, CancellationToken cancellationToken)
        {
            try
            {
                FtpPath listDir = dir;

                bool isRootPath = dir.IsRoot;

                if (!isRootPath)
                {
                    listDir = dir.Parent;
                }

                ImmutableArray<FtpPath> items = await ListDirectoryAsync(listDir, false, cancellationToken);

                if (isRootPath)
                {
                    return true;
                }

                FtpPath[] foundPaths = items.Where(item => item.ContainsPath(dir)).ToArray();

                if (foundPaths.Length == 0)
                {
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

        public async Task UploadFileAsync(
            FtpPath filePath,
            FileInfo sourceFile,
            CancellationToken cancellationToken = default)
        {
            FtpRequest request = CreateRequest(filePath, FtpMethod.UploadFile);

            request.Request.ContentLength = sourceFile.Length;

            using (var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open))
            {
                using (Stream requestStream = await request.Request.GetRequestStreamAsync())
                {
                    await sourceStream.CopyToAsync(requestStream, DefaultBufferSize, cancellationToken);
                }
            }

            using (FtpResponse response = await GetFtpResponseAsync(request))
            {
                if (response.Response.StatusCode != FtpStatusCode.ClosingData)
                {
                    throw new FtpException($"Could not upload file '{filePath}'", response.Response.StatusCode);
                }
            }
        }

        public async Task DeleteFileAsync([NotNull] FtpPath filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using (FtpResponse response = await CreateFtpResponseAsync(filePath, FtpMethod.DeleteFile))
            {
                if (response.Response.StatusCode != FtpStatusCode.FileActionOK)
                {
                    throw new FtpException($"Could not delete file '{filePath}'");
                }
            }
        }

        public async Task DeleteDirectoryAsync([NotNull] FtpPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (FtpResponse response = await CreateFtpResponseAsync(path, FtpMethod.RemoveDirectory))
            {
                if (response.Response.StatusCode != FtpStatusCode.FileActionOK)
                {
                    throw new FtpException($"Could not delete directory '{path}'");
                }
            }
        }

        public async Task FileExistsAsync([NotNull] FtpPath filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using (FtpResponse response = await CreateFtpResponseAsync(filePath, FtpMethod.GetFileSize))
            {
                if (response.Response.StatusCode != FtpStatusCode.CommandOK)
                {
                    throw new FtpException($"Could not delete directory '{filePath}'");
                }
            }
        }

        public async Task CreateDirectoryAsync([NotNull] FtpPath directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            using (FtpResponse response = await CreateFtpResponseAsync(directoryPath, FtpMethod.MakeDirectory))
            {
                if (response.Response.StatusCode != FtpStatusCode.PathnameCreated)
                {
                    throw new FtpException(
                        $"Could not created directory {directoryPath}, status {response.Response.StatusCode}");
                }
            }
        }

        public async Task<ImmutableArray<FtpPath>> ListDirectoryAsync(
            [NotNull] FtpPath path,
            bool recursive = true,
            CancellationToken cancellationToken = default)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            try
            {
                var lines = new List<FtpPath>();

                if (cancellationToken.IsCancellationRequested)
                {
                    return lines.ToImmutableArray();
                }

                using (FtpResponse response = await CreateFtpResponseAsync(path, FtpMethod.ListDirectoryDetails))
                {
                    var currentLines = new List<string>();
                    using (var reader = new StreamReader(response.Response.GetResponseStream() ??
                                                         throw new FtpException("FTP response stream is null")))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null &&
                               !cancellationToken.IsCancellationRequested)
                        {
                            currentLines.Add(line);
                        }
                    }

                    foreach (string currentLine in currentLines)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        FtpPath currentFtpPath = ParseLine(currentLine);

                        string currentFullPath = $"{path.Path.TrimEnd('/')}/{currentFtpPath.Path}";

                        if (currentFtpPath.Type == FileSystemType.Directory)
                        {
                            lines.Add(new FtpPath(currentFullPath, FileSystemType.Directory));

                            if (recursive)
                            {
                                ImmutableArray<FtpPath> immutableArrays = await ListDirectoryAsync(
                                    new FtpPath(currentFullPath, currentFtpPath.Type),
                                    true,
                                    cancellationToken);

                                lines.AddRange(immutableArrays);
                            }
                        }
                        else
                        {
                            lines.Add(new FtpPath(currentFullPath, FileSystemType.File));
                        }
                    }
                }

                return lines.ToImmutableArray();
            }
            catch (WebException ex)
            {
                throw new FtpException($"Could not list files for directory '{path}'", ex);
            }
        }

        public static FtpHandler CreateWithPublishSettings(string publishSettingsFile, string uriPath = null)
        {
            FtpPublishSettings ftpPublishSettings = FtpPublishSettings.Load(publishSettingsFile);

            var credentials = new NetworkCredential(ftpPublishSettings.UserName, ftpPublishSettings.Password);

            Uri fullUri = ftpPublishSettings.FtpBaseUri;

            if (!string.IsNullOrWhiteSpace(uriPath))
            {
                var builder = new UriBuilder(fullUri) { Path = uriPath };

                fullUri = builder.Uri;
            }

            return new FtpHandler(fullUri, credentials);
        }

        public async Task<FtpSummary> UploadDirectoryAsync(
            RuleConfiguration ruleConfiguration,
            DirectoryInfo sourceDirectory,
            DirectoryInfo baseDirectory,
            CancellationToken cancellationToken)
        {
            var dir = new FtpPath(PathHelper.RelativePath(sourceDirectory, baseDirectory),
                FileSystemType.Directory);

            var summary = new FtpSummary();

            if (!await DirectoryExistsAsync(dir, cancellationToken))
            {
                await CreateDirectoryAsync(dir);
                summary.CreatedDirectories.Add(dir.Path);
            }

            foreach (FileInfo fileInfo in sourceDirectory.GetFiles())
            {
                var sourceFile =
                    new FtpPath(PathHelper.RelativePath(fileInfo, baseDirectory), FileSystemType.File);

                await UploadFileAsync(sourceFile, fileInfo, cancellationToken);

                summary.CreatedFiles.Add(sourceFile.Path);
            }

            foreach (DirectoryInfo subDirectory in sourceDirectory.GetDirectories())
            {
                FtpSummary summary1 =
                    await UploadDirectoryAsync(ruleConfiguration, subDirectory, baseDirectory, cancellationToken);
                summary.Add(summary1);
            }

            return summary;
        }

        public async Task<IDeploymentChangeSummary> PublishAsync(
            RuleConfiguration ruleConfiguration,
            DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken)
        {
            var deploymentChangeSummary = new FtpSummary();
            ImmutableArray<FtpPath> fileSystemItems =
                await ListDirectoryAsync(FtpPath.Root, cancellationToken: cancellationToken);

            var excludedSegments = new List<FtpPath>();

            foreach (FtpPath fileSystemItem in fileSystemItems
                .Where(fileSystemItem => fileSystemItem.Type == FileSystemType.File))
            {
                if (ruleConfiguration.AppDataSkipDirectiveEnabled)
                {
                    if (fileSystemItem.IsAppDataDirectoryOrFile)
                    {
                        excludedSegments.Add(fileSystemItem);
                        continue;
                    }
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

            foreach (FtpPath item in fileSystemItems
                .Where(fileSystemItem => fileSystemItem.Type == FileSystemType.Directory)
                .OrderByDescending(s => s.Path))
            {
                if (ruleConfiguration.AppDataSkipDirectiveEnabled)
                {
                    if (item.IsAppDataDirectoryOrFile)
                    {
                        excludedSegments.Add(item);
                        continue;
                    }
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

            FtpSummary uploadDirectoryAsync =
                await UploadDirectoryAsync(ruleConfiguration, sourceDirectory, sourceDirectory, cancellationToken);

            deploymentChangeSummary.Add(uploadDirectoryAsync);

            return deploymentChangeSummary;
        }
    }
}
