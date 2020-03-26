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

        public DeploymentObject(DeploymentWellKnownProvider provider,
            string path,
            DeploymentBaseOptions deploymentBaseOptions,
            ILogger logger)
        {
            _logger = logger;
            Provider = provider;
            Path = path;
            DeploymentBaseOptions = deploymentBaseOptions;
        }

        public DeploymentWellKnownProvider Provider { get; }

        public string Path { get; }

        public DeploymentBaseOptions DeploymentBaseOptions { get; }

        public void Dispose()
        {
        }

        public async Task<WebDeployChangeSummary> SyncTo(
            DeploymentWellKnownProvider destinationProvider,
            string destinationPath,
            DeploymentBaseOptions deploymentBaseOptions,
            DeploymentSyncOptions syncOptions,
            CancellationToken cancellationToken = default)
        {

            if (Provider == DeploymentWellKnownProvider.ContentPath &&
                destinationProvider == DeploymentWellKnownProvider.ContentPath)
            {
            }
            else
            {
                throw new NotSupportedException(
                    $"The current provider {Provider.Name} to provider {destinationProvider.Name} is not supported");
            }

            void Configure(List<string> arguments)
            {
                string dest = CreateDestination(deploymentBaseOptions);

                arguments.AddRange(new[]
                {
                    "-verb:sync",
                    $"-source:contentPath=\"{Path}\"",
                    dest,
                    "-verbose"
                });

                if (!string.IsNullOrWhiteSpace(destinationPath))
                {
                    string destinationParameter = GetDestinationParameter(deploymentBaseOptions.SiteName, null);
                    arguments.Add(destinationParameter);
                }
            }

            return await SyncToInternal(
                deploymentBaseOptions,
                syncOptions,
                Configure,
                cancellationToken);
        }

        private static string GetDestinationParameter(string siteName, string destinationPath)
        {
            string path = string.IsNullOrWhiteSpace(destinationPath) ? null : $"/{destinationPath.TrimStart('/')}";
            string destinationParameter = $"-setParam:kind=ProviderPath,scope=contentPath,value=\"{siteName}{path}\"";
            return destinationParameter;
        }

        private static string CreateDestination(DeploymentBaseOptions deploymentBaseOptions)
        {
            string dest = "-dest:contentPath";

            if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.ComputerName))
            {
                string url;

                if (!deploymentBaseOptions.ComputerName.StartsWith("https://"))
                {
                    url = $"https://{deploymentBaseOptions.ComputerName}";
                }
                else
                {
                    url = deploymentBaseOptions.ComputerName;
                }

                if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.SiteName))
                {
                    url += $"/msdeploy.axd?site={Uri.EscapeDataString(deploymentBaseOptions.SiteName)}";
                }

                dest += $",computername=\"{url}\"";
            }

            if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.UserName))
            {
                dest += $",username=\"{deploymentBaseOptions.UserName}\"";
            }

            if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.Password))
            {
                dest += $",password=\"{deploymentBaseOptions.Password}\"";
            }

            dest += $",authtype=\"{deploymentBaseOptions.AuthenticationType.Name}\"";
            return dest;
        }

        public async Task<WebDeployChangeSummary> SyncTo(DeploymentBaseOptions baseOptions,
            DeploymentSyncOptions syncOptions, CancellationToken cancellationToken = default)
        {
            void Configure(List<string> arguments)
            {
                string dest = CreateDestination(baseOptions);

                arguments.AddRange(new[]
                {
                    "-verb:delete",
                    dest,
                    "-verbose"
                });

                if (!string.IsNullOrWhiteSpace(Path))
                {
                    string destinationParameter = GetDestinationParameter(DeploymentBaseOptions.SiteName, Path);
                    arguments.Add(destinationParameter);
                }
            }

            return await SyncToInternal(baseOptions,
                syncOptions,
                Configure,
                cancellationToken);
        }

        private async Task<WebDeployChangeSummary> SyncToInternal(
            DeploymentBaseOptions deploymentBaseOptions,
            DeploymentSyncOptions syncOptions,
            Action<List<string>> onConfigureArgs,
            CancellationToken cancellationToken = default)
        {
            string exePath = @"C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe";

            //var exePath = @"C:\Tools\Arbor.ProcessDiagnostics\ConsoleApp1.exe";

            var arguments = new List<string>();

            long addedFiles = 0;
            long deletedFiles = 0;
            long addedDirectories = 0;
            long deletedDirectories = 0;

                if (syncOptions.WhatIf)
                {
                    arguments.Add("-whatif");
                }

                if (syncOptions.UseChecksum)
                {
                    arguments.Add("-useCheckSum");
                }

                if (DeploymentBaseOptions.AllowUntrusted || deploymentBaseOptions.AllowUntrusted)
                {
                    arguments.Add("-allowUntrusted");
                }

                onConfigureArgs(arguments);

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
                    }
                    else if (message.Contains("adding file", StringComparison.OrdinalIgnoreCase))
                    {
                        addedFiles++;
                    }
                    else if (message.Contains("adding directory", StringComparison.OrdinalIgnoreCase))
                    {
                        addedDirectories++;
                    }
                }

                void LogError(string message, string category)
                {
                    _logger.Error("{Message}", message);
                }

                var exitCode = await ProcessRunner.ExecuteProcessAsync(
                    exePath,
                    arguments,
                    Log,
                    standardErrorAction: LogError,
                    formatArgs: false,
                    cancellationToken: cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    _logger.Error("MSDeploy.exe Failed with exit code {ExitCode}", exitCode.Code);
                }

                return new WebDeployChangeSummary
                {
                    AddedFiles = addedFiles,
                    AddedDirectories = addedDirectories,
                    DeletedFiles = deletedFiles,
                    DeletedDirectories = deletedDirectories,
                    ExitCode = exitCode.Code
                };
        }
    }
}