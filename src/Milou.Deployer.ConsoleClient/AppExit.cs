using System;
using System.Diagnostics;
using Milou.Deployer.Core.Processes;

namespace Milou.Deployer.ConsoleClient
{
    public static class AppExit
    {
        public static ExitCode ExitSuccess()
        {
            BreakApp();

            return ExitCode.Success;
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

        public static ExitCode Exit(ExitCode exitCode)
        {
            BreakApp();
            return exitCode;
        }

        public static ExitCode ExitFailure()
        {
            BreakApp();

            return ExitCode.Failure;
        }
    }
}