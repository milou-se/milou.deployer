using System;
using System.Diagnostics;
using Milou.Deployer.Core.Processes;
using Serilog;

namespace Milou.Deployer.ConsoleClient
{
    internal sealed class AppExit
    {
        private readonly ILogger _logger;

        public AppExit(ILogger logger)
        {
            _logger = logger;
        }

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
                    Console.WriteLine("Press ENTER to continue");
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
