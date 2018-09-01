using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Tooler;
using Serilog;
using Serilog.Core;

namespace Milou.Deployer.Bootstrapper
{
    public sealed class App : IDisposable
    {
        private readonly ILogger _logger;
        private readonly NuGetPackageInstaller _packageInstaller;

        private App(NuGetPackageInstaller packageInstaller, ILogger logger)
        {
            _packageInstaller = packageInstaller;
            _logger = logger;
        }

        public static Task<App> CreateAsync(string[] args)
        {
            Logger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            return Task.FromResult(new App(new NuGetPackageInstaller(), logger));
        }

        public void Dispose()
        {
        }

        public async Task<int> ExecuteAsync(ImmutableArray<string> appArgs)
        {
            NuGetPackageInstallResult nuGetPackageInstallResult =
                await _packageInstaller.InstallPackageAsync(Constants.PackageId);

            var deployerToolFile = new FileInfo(Path.Combine(nuGetPackageInstallResult.PackageDirectory.FullName,
                "tools",
                "Milou.Deployer.ConsoleClient.exe"));

            int exitCode;
            using (var process = new Process())
            {
                process.StartInfo.Arguments = string.Join(" ", appArgs.Select(arg => "\"{arg}\""));
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = deployerToolFile.FullName;

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        _logger.Information(args.Data);
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
