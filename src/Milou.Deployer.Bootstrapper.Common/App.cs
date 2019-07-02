using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Tooler;
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

            logger = logger ?? new LoggerConfiguration().WriteTo.Console().CreateLogger();

            httpClient = httpClient ?? new HttpClient();
            var nuGetDownloadClient = new NuGetDownloadClient();
            var nuGetCliSettings = new NuGetCliSettings();
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

        public async Task<NuGetPackageInstallResult> ExecuteAsync(
            ImmutableArray<string> appArgs,
            CancellationToken cancellationToken = default,
            TimeSpan? processTimeout = default)
        {
            if (appArgs.IsDefault)
            {
                throw new ArgumentException("Arguments cannot be default", nameof(appArgs));
            }

            NuGetPackageInstallResult nuGetPackageInstallResult;

            var nuGetPackageId = new NuGetPackageId(Constants.PackageId);

            try
            {
                bool allowPreRelease = appArgs.Any(arg =>
                    arg.Equals(Constants.AllowPreRelease, StringComparison.OrdinalIgnoreCase));

                _logger.Debug("Pre-release flag set to {Flag}", allowPreRelease);

                var nuGetPackage = new NuGetPackage(nuGetPackageId, NuGetPackageVersion.LatestAvailable);

                _logger.Debug("Downloading package {Package}", nuGetPackage);

                nuGetPackageInstallResult =
                    await _packageInstaller.InstallPackageAsync(
                        nuGetPackage,
                        new NugetPackageSettings(allowPreRelease),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not download NuGet packages");
                return NuGetPackageInstallResult.Failed(nuGetPackageId);
            }

            if (nuGetPackageInstallResult.PackageDirectory is null || nuGetPackageInstallResult.SemanticVersion is null)
            {
                _logger.Error("Could not download NuGet package {PackageId}", nuGetPackageId);
                return NuGetPackageInstallResult.Failed(nuGetPackageId);
            }

            if (appArgs.Any(arg => arg.Equals(Constants.DownloadOnly, StringComparison.OrdinalIgnoreCase)))
            {
                return nuGetPackageInstallResult;
            }

            var deployerToolFile = new FileInfo(Path.Combine(nuGetPackageInstallResult.PackageDirectory.FullName,
                "tools",
                "net472",
                "Milou.Deployer.ConsoleClient.exe"));

            if (!deployerToolFile.Exists)
            {
                string[] existingFiles =
                    nuGetPackageInstallResult.PackageDirectory.GetFiles("", SearchOption.AllDirectories)
                        .Select(file => file.FullName).ToArray();

                _logger.Error("The extracted file '{File}' does not exist, existing files {ExistingFiles}",
                    deployerToolFile.FullName,
                    existingFiles);

                return NuGetPackageInstallResult.Failed(nuGetPackageId);
            }

            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(deployerToolFile.FullName,
                    appArgs,
                    standardOutLog: (message, category) =>
                        _logger.Information("{Category} {Message}", category, message),
                    standardErrorAction: (message, category) =>
                        _logger.Error("{Category} {Message}", category, message),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                _logger.Error("The process {Process} {Arguments} failed with exit code {ExitCode}",
                    deployerToolFile,
                    appArgs,
                    exitCode);

                return NuGetPackageInstallResult.Failed(nuGetPackageId);
            }

            return nuGetPackageInstallResult;
        }
    }
}
