using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public interface IFtpHandler: IDisposable
    {
        Task<bool> DirectoryExistsAsync([NotNull] FtpPath dir, CancellationToken cancellationToken);

        Task UploadFileAsync(
            [NotNull] FtpPath filePath,
            [NotNull] FileInfo sourceFile,
            CancellationToken cancellationToken = default);

        Task DeleteFileAsync([NotNull] FtpPath filePath, CancellationToken cancellationToken);
        Task DeleteDirectoryAsync([NotNull] FtpPath path, CancellationToken cancellationToken);
        Task<bool> FileExistsAsync([NotNull] FtpPath filePath, CancellationToken cancellationToken);
        Task CreateDirectoryAsync([NotNull] FtpPath directoryPath, CancellationToken cancellationToken);

        Task<ImmutableArray<FtpPath>> ListDirectoryAsync(
            [NotNull] FtpPath path,
            CancellationToken cancellationToken = default);

        Task<DeploySummary> UploadDirectoryAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            [NotNull] DirectoryInfo baseDirectory,
            [NotNull] FtpPath basePath,
            CancellationToken cancellationToken);

        Task<DeploySummary> PublishAsync(
            [NotNull] RuleConfiguration ruleConfiguration,
            [NotNull] DirectoryInfo sourceDirectory,
            CancellationToken cancellationToken);
    }
}