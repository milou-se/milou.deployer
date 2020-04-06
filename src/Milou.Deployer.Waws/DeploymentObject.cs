using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Milou.Deployer.Core.Deployment;
using Serilog;

namespace Milou.Deployer.Waws
{
    internal sealed class DeploymentObject : IDisposable
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

        public async Task<DeploySummary> SyncTo(
            DeploymentWellKnownProvider destinationProvider,
            string destinationPath,
            DeploymentBaseOptions deploymentBaseOptions,
            DeploymentSyncOptions syncOptions,
            CancellationToken cancellationToken = default)
        {
            Action<List<string>> action;

            if (Provider == DeploymentWellKnownProvider.ContentPath &&
                destinationProvider == DeploymentWellKnownProvider.ContentPath)
            {
                action = arguments =>
                {
                    string dest = CreateDestination(deploymentBaseOptions);

                    arguments.AddRange(new[] {"-verb:sync", $"-source:contentPath=\"{Path}\"", dest, "-verbose"});

                    if (!string.IsNullOrWhiteSpace(destinationPath) &&
                        !string.IsNullOrWhiteSpace(deploymentBaseOptions.SiteName))
                    {
                        string destinationParameter = GetDestinationParameter(deploymentBaseOptions.SiteName, null);
                        arguments.Add(destinationParameter);
                    }
                };
            }
            else  if (Provider == DeploymentWellKnownProvider.DirPath &&
                     destinationProvider == DeploymentWellKnownProvider.DirPath)
            {
                action = arguments =>
                {
                    string dest = CreateDestination(deploymentBaseOptions) + "=" + destinationPath;

                    arguments.AddRange(new[] { "-verb:sync", $"-source:dirPath=\"{Path}\"", dest, "-verbose" });
                };
            }
            else
            {
                throw new NotSupportedException(
                    $"The current provider {Provider.Name} to provider {destinationProvider.Name} is not supported");
            }

            return await SyncToInternal(
                deploymentBaseOptions,
                syncOptions,
                action,
                cancellationToken);
        }

        private static string GetDestinationParameter(string siteName, string? destinationPath)
        {
            string? path = string.IsNullOrWhiteSpace(destinationPath) ? null : $"/{destinationPath.TrimStart('/')}";
            string destinationParameter = $"-setParam:kind=ProviderPath,scope=contentPath,value=\"{siteName}{path}\"";
            return destinationParameter;
        }

        private static string CreateDestination(DeploymentBaseOptions deploymentBaseOptions)
        {
            string dest = "-dest:";

            if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.ComputerName))
            {
                dest += "contentPath";

                string url;

                url = !deploymentBaseOptions.ComputerName.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? $"https://{deploymentBaseOptions.ComputerName}"
                    : deploymentBaseOptions.ComputerName;

                if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.SiteName))
                {
                    url += $"/msdeploy.axd?site={Uri.EscapeDataString(deploymentBaseOptions.SiteName)}";
                }

                dest += $",computername=\"{url}\"";

                if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.UserName))
                {
                    dest += $",username=\"{deploymentBaseOptions.UserName}\"";
                }

                if (!string.IsNullOrWhiteSpace(deploymentBaseOptions.Password))
                {
                    dest += $",password=\"{deploymentBaseOptions.Password}\"";
                }

                dest += $",authtype=\"{deploymentBaseOptions.AuthenticationType.Name}\"";
            }
            else
            {
                dest += "dirPath";
            }

            return dest;
        }

        public async Task<DeploySummary> SyncTo(DeploymentBaseOptions baseOptions,
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

                if (!string.IsNullOrWhiteSpace(Path) && !string.IsNullOrWhiteSpace(DeploymentBaseOptions.SiteName))
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

        private async Task<DeploySummary> SyncToInternal(
            DeploymentBaseOptions deploymentBaseOptions,
            DeploymentSyncOptions syncOptions,
            Action<List<string>> onConfigureArgs,
            CancellationToken cancellationToken = default)
        {
            string exePath = @"C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe";

            //var exePath = @"C:\Tools\Arbor.ProcessDiagnostics\ConsoleApp1.exe";

            var arguments = new List<string>();

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

            var syncToInternal = new DeploySummary();
            onConfigureArgs(arguments);

            static string ParseEntry(string entry)
            {
                ReadOnlySpan<char> asSpan = entry.AsSpan();

                int start = asSpan.IndexOf('(') + 1;
                int end = asSpan.LastIndexOf(')');
                int length = end - start;

                if (length <= 0)
                {

                }

                return asSpan.Slice(start, length).ToString();
            }

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

                if (message.Contains("deleting file (", StringComparison.OrdinalIgnoreCase))
                {
                    syncToInternal.Deleted.Add(ParseEntry(message));
                }
                else if (message.Contains("adding file (", StringComparison.OrdinalIgnoreCase))
                {
                    syncToInternal.CreatedFiles.Add(ParseEntry(message));
                }
                else if (message.Contains("adding directory (", StringComparison.OrdinalIgnoreCase))
                {
                    syncToInternal.CreatedDirectories.Add(ParseEntry(message));
                }
                else if (message.Contains("updating file (", StringComparison.OrdinalIgnoreCase))
                {
                    syncToInternal.UpdatedFiles.Add(ParseEntry(message));
                }
            }

            void LogError(string message, string category)
            {
                _logger.Error("{Message}", message);
            }

            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(
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

            return syncToInternal;
        }
    }
}