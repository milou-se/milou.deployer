using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            bool disposeNested = true)
        {
            logger = logger ?? new LoggerConfiguration().WriteTo.Console().CreateLogger();

            httpClient = httpClient ?? new HttpClient();
            var nuGetPackageInstaller = new NuGetPackageInstaller(new NuGetDownloadClient(httpClient));

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

        public async Task<int> ExecuteAsync(
            ImmutableArray<string> appArgs,
            CancellationToken cancellationToken = default)
        {
            if (appArgs.IsDefault)
            {
                throw new ArgumentException("Arguments cannot be default", nameof(appArgs));
            }

            NuGetPackageInstallResult nuGetPackageInstallResult;

            try
            {
                bool allowPreRelease = appArgs.Any(arg =>
                    arg.Equals(Constants.AllowPreRelease, StringComparison.OrdinalIgnoreCase));

                nuGetPackageInstallResult =
                    await _packageInstaller.InstallPackageAsync(
                        new NuGetPackage(new NuGetPackageId(Constants.PackageId), NuGetPackageVersion.LatestAvailable),
                        new NugetPackageSettings(allowPreRelease),
                        cancellationToken: cancellationToken);
            }

            catch (Exception ex)
            {
                _logger.Error(ex, "Could not download NuGet packages");
                return 1;
            }

            if (appArgs.Any(arg => arg.Equals(Constants.DownloadOnly, StringComparison.OrdinalIgnoreCase)))
            {
                return 0;
            }

            var deployerToolFile = new FileInfo(Path.Combine(nuGetPackageInstallResult.PackageDirectory.FullName,
                "tools",
                "net472",
                "Milou.Deployer.ConsoleClient.exe"));

            int exitCode;

            using (var process = new Process())
            {
                process.StartInfo.Arguments = string.Join(" ", appArgs.Select(arg => $"\"{arg}\""));
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = deployerToolFile.FullName;
                process.EnableRaisingEvents = true;

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        _logger.Information("{Message}", args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        _logger.Error("{Error}", args.Data);
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();

                exitCode = process.ExitCode;
            }

            return exitCode;
        }
    }
}
