using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Arbor.Processing;
using Serilog;

namespace Milou.Deployer.DeployerApp
{
    internal sealed class AppExit
    {
        private const string ExitMessage = "Press ENTER to continue";
        private readonly ILogger _logger;

        public AppExit([NotNull] ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public ExitCode ExitSuccess()
        {
            _logger.Information("Application was successful, {ExitCode}", ExitCode.Success);

            BreakApp();

            return ExitCode.Success;
        }

        public ExitCode Exit(ExitCode exitCode)
        {
            if (exitCode.IsSuccess)
            {
                return ExitSuccess();
            }

            return ExitFailure(exitCode.Code);
        }

        public ExitCode ExitFailure(int exitCode = 1)
        {
            _logger.Error("Application failed, {ExitCode}", exitCode);

            BreakApp();

            return ExitCode.Failure;
        }

        private static void BreakApp()
        {
            if (Debugger.IsAttached)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine(ExitMessage);
                    Console.ReadLine();
                }
                else
                {
                    Debugger.Break();
                }
            }
        }
    }
}