using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Tooler;

using Milou.Deployer.Core.Cli;
using Milou.Deployer.Core.Logging;

using NuGet.Versioning;

using Serilog;

namespace Milou.Deployer.Bootstrapper.Common
{
    public sealed class App : IDisposable
    {
        private readonly bool _disposeNested;
        private HttpClient _httpClient;
        private ILogger _logger;
        private NuGetPackageInstaller _packageInstaller;

        private App(NuGetPackageInstaller packageInstaller, ILogger logger, HttpClient httpClient, bool disposeNested)
        {
            _packageInstaller = packageInstaller ?? throw new ArgumentNullException(nameof(packageInstaller));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeNested = disposeNested;
        }

        public static Task<App> CreateAsync(
            string[] args,
            ILogger logger = default,
            HttpClient httpClient = default,
            bool disposeNested = true,
            CancellationToken cancellationToken = default)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var appArgs = args.ToImmutableArray();

            logger ??= new LoggerConfiguration().WriteTo.Console().CreateLogger();

            string nugetSource = GetNuGetSource(appArgs);
            string nugetConfig = GetNuGetConfig(appArgs);
            string nugetExePath = GetNuGetExePath(appArgs);

            httpClient ??= new HttpClient();
            var nuGetDownloadClient = new NuGetDownloadClient();
            var nuGetCliSettings = new NuGetCliSettings(nugetSource, nugetConfig, nugetExePath);
            var nuGetDownloadSettings = new NuGetDownloadSettings();
            var nuGetPackageInstaller = new NuGetPackageInstaller(
                nuGetDownloadClient,
                nuGetCliSettings,
                nuGetDownloadSettings,
                logger);

            return Task.FromResult(new App(nuGetPackageInstaller, logger, httpClient, disposeNested));
        }

        public void Dispose()
        {
            if (_disposeNested)
            {
                _packageInstaller = null;
                _httpClient?.Dispose();
                _httpClient = null;

                if (_logger is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _logger = null;
            }
        }

        private async Task<(NuGetPackageInstallResult,FileInfo)> GetDeployerExePathAsync(ImmutableArray<string> appArgs, NuGetPackageId nuGetPackageId, CancellationToken cancellationToken)
        {
            NuGetPackageInstallResult nuGetPackageInstallResult;

            FileInfo deployerToolFile = GetDeployerExeFromArgs(appArgs);

            if (deployerToolFile is { })
            {
                nuGetPackageInstallResult = new NuGetPackageInstallResult(
                    nuGetPackageId,
                    new SemanticVersion(1, 0, 0),
                    deployerToolFile.Directory);
            }
            else
            {
                string nugetSource = GetNuGetSource(appArgs);
                string nugetConfig = GetNuGetConfig(appArgs);

                try
                {
                    bool allowPreRelease = appArgs.Any(
                        arg => arg.Equals(Constants.AllowPreRelease, StringComparison.OrdinalIgnoreCase));

                    _logger.Debug("Pre-release flag set to {Flag}", allowPreRelease);

                    var nuGetPackage = new NuGetPackage(nuGetPackageId, NuGetPackageVersion.LatestAvailable);

                    _logger.Debug("Downloading package {Package}", nuGetPackage);

                    nuGetPackageInstallResult = await _packageInstaller.InstallPackageAsync(
                                                    nuGetPackage,
                                                    new NugetPackageSettings(allowPreRelease, nugetSource, nugetConfig),
                                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Error(ex, "Could not download NuGet packages");
                    throw new InvalidOperationException(NuGetPackageInstallResult.Failed(nuGetPackageId).ToString());
                }

                if (nuGetPackageInstallResult.PackageDirectory is null
                    || nuGetPackageInstallResult.SemanticVersion is null)
                {
                    _logger.Error("Could not download NuGet package {PackageId}", nuGetPackageId);
                    return (NuGetPackageInstallResult.Failed(nuGetPackageId), (FileInfo)null);
                }

                if (IsDownloadOnly(appArgs))
                {
                    return (nuGetPackageInstallResult, (FileInfo)null);
                }

                string deployerToolFilePath = Path.Combine(
                    nuGetPackageInstallResult.PackageDirectory.FullName,
                    "tools",
                    "net472",
                    "Milou.Deployer.ConsoleClient.exe");

                deployerToolFile = new FileInfo(deployerToolFilePath);
            }

            if (!deployerToolFile.Exists)
            {
                string[] existingFiles =
                    nuGetPackageInstallResult.PackageDirectory.GetFiles("", SearchOption.AllDirectories)
                        .Select(file => file.FullName).ToArray();

                _logger.Error("The extracted file '{File}' does not exist, existing files {ExistingFiles}",
                    deployerToolFile,
                    existingFiles);

                return (NuGetPackageInstallResult.Failed(nuGetPackageId), (FileInfo)null);
            }

            return (nuGetPackageInstallResult, deployerToolFile);
        }

        private static FileInfo GetDeployerExeFromArgs(ImmutableArray<string> appArgs)
        {
            string exePath = appArgs.GetArgumentValueOrDefault("deployer-exe");

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return null;
            }

            return new FileInfo(exePath);
        }

        private static bool IsDownloadOnly(ImmutableArray<string> appArgs) => appArgs.Any(arg => arg.Equals(Constants.DownloadOnly, StringComparison.OrdinalIgnoreCase));

        public async Task<NuGetPackageInstallResult> ExecuteAsync(
            ImmutableArray<string> appArgs,
            TimeSpan? processTimeout = default,
            CancellationToken cancellationToken = default)
        {
            if (appArgs.IsDefault)
            {
                throw new ArgumentException("Arguments cannot be default", nameof(appArgs));
            }

            var nuGetPackageId = new NuGetPackageId(Constants.PackageId);

            var (nugetInstallResult, deployerExeFileInfo) = await GetDeployerExePathAsync(appArgs, nuGetPackageId, cancellationToken);

            if (IsDownloadOnly(appArgs))
            {
                return nugetInstallResult;
            }

            if (deployerExeFileInfo is null)
            {
                return nugetInstallResult;
            }

            string deployerExePath = deployerExeFileInfo.FullName;

            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(deployerExePath,
                    appArgs,
                    standardOutLog: (message, category) => _logger.ParseAndLog(message, category),
                    standardErrorAction: (message, category) =>
                        _logger.Error("{Category} {Message}", category, message),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                _logger.Error("The process {Process} {Arguments} failed with exit code {ExitCode}",
                    deployerExePath,
                    appArgs,
                    exitCode);

                return NuGetPackageInstallResult.Failed(nuGetPackageId);
            }

            return nugetInstallResult;
        }

        private static string GetNuGetSource(ImmutableArray<string> appArgs)
        {
            string nugetSource = appArgs.GetArgumentValueOrDefault("nuget-source");
            return nugetSource;
        }

        private static string GetNuGetExePath(ImmutableArray<string> appArgs)
        {
            var exePath = appArgs.GetArgumentValueOrDefault("nuget-exe");

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                exePath = null;
            }

            return exePath;
        }

        private static string GetNuGetConfig(ImmutableArray<string> appArgs)
        {
            var nugetConfig = appArgs.GetArgumentValueOrDefault("nuget-config");

            if (string.IsNullOrWhiteSpace(nugetConfig) || !File.Exists(nugetConfig))
            {
                nugetConfig = null;
            }

            return nugetConfig;
        }
    }
}
