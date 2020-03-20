using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Serilog;

namespace Milou.Deployer.Waws
{
    internal class DeploymentObject : IDisposable
    {
        private readonly ILogger _logger;
        public DeploymentWellKnownProvider Provider { get; }

        public string Path { get; }

        public DeploymentBaseOptions DeploymentBaseOptions { get; }

        public DeploymentObject(DeploymentWellKnownProvider provider, string path,
            DeploymentBaseOptions deploymentBaseOptions, ILogger logger)
        {
            _logger = logger;
            Provider = provider;
            Path = path;
            DeploymentBaseOptions = deploymentBaseOptions;
        }

        public WebDeployChangeSummary SyncTo(DeploymentBaseOptions baseOptions, DeploymentSyncOptions syncOptions) =>
            throw new NotImplementedException();

        public void Dispose()
        {
        }

        public async Task<WebDeployChangeSummary> SyncTo(DeploymentWellKnownProvider provider, string destinationPath,
            DeploymentBaseOptions deploymentBaseOptions, DeploymentSyncOptions syncOptions, CancellationToken cancellationToken = default)
        {
            string exePath = @"C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe";

            var arguments = new List<string>()
            {
            };

            long addedFiles = 0;
            long deletedFiles = 0;
            long addedDirectories = 0;
            long deletedDirectories = 0;


            if (Provider == DeploymentWellKnownProvider.DirPath && provider == DeploymentWellKnownProvider.DirPath)
            {
                arguments.AddRange(new[]
                {
                    "-verb:sync", $"-source:dirPath={Path}", $"-dest:dirPath={destinationPath}", "-verbose"
                });

                void Log(string message, string category)
                {
                    if (message.StartsWith("Verbose: ", StringComparison.Ordinal))
                    {
                        _logger.Verbose("{Message}", message.Substring(9));
                    }
                    else if (message.StartsWith("Debug: ", StringComparison.Ordinal))
                    {
                        _logger.Debug("{Message}", message.Substring(7));
                    }
                    else
                    {
                        _logger.Information("{Message}", message.Substring(6));
                    }

                    if (message.Contains("deleting file", StringComparison.OrdinalIgnoreCase))
                    {
                        deletedFiles++;
                    } else if (message.Contains("adding file", StringComparison.OrdinalIgnoreCase))
                    {
                        addedFiles++;
                    }else if (message.Contains("adding directory", StringComparison.OrdinalIgnoreCase))
                    {
                        addedDirectories++;
                    }
                }

            void LogError(string message, string category) => _logger.Error("{Message}", message);

                var exitCode = await ProcessRunner.ExecuteProcessAsync(
                    exePath,
                    arguments,
                    Log,
                    standardErrorAction: LogError,
                    formatArgs: false,
                    cancellationToken: cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    return new WebDeployChangeSummary();
                }

                return new WebDeployChangeSummary()
                {
                    AddedFiles = addedFiles,
                    AddedDirectories = addedDirectories,
                    DeletedFiles = deletedFiles,
                    DeletedDirectories = deletedDirectories
                };
            }

            throw new NotSupportedException();
        }
    }
}