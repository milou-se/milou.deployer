using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.ConsoleClient;
using Milou.Deployer.Core.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class AppArgTests
    {
        private const string TempKey = ConfigurationKeys.TempDirectory;

        [Fact]
        public async Task ExitCodeShouldBeNonZeroForInvalidArgs()
        {
            string[] args = {"--arg1", "--arg2", "asd", "123"};

            CancellationToken cancellationToken = default;
            ILogger logger = Logger.None;

            int exitCode;

            using (DeployerApp deployerApp = await
                AppBuilder.BuildAppAsync(args, logger, cancellationToken))
            {
                exitCode = await deployerApp.ExecuteAsync(args, cancellationToken);
            }

            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public async Task LogLevelShouldBeSetWhenArgIsUsed()
        {
            string[] args = { $"{ConfigurationKeys.LogLevel}=error" };

            CancellationToken cancellationToken = default;

            LoggingLevelSwitch level;
            using (DeployerApp deployerApp = await
                AppBuilder.BuildAppAsync(args, null, cancellationToken))
            {
                level = deployerApp.LevelSwitch;
            }

            Assert.NotEqual(LogEventLevel.Error, level.MinimumLevel);
        }

        [Fact]
        public async Task TempPathShouldBeSetWhenDefined()
        {
            string tempPath;
            string oldTemp = default;

            Directory.CreateDirectory(@"C:\temp");

            try
            {
                oldTemp = Path.GetTempPath();
                string[] args = { $"-{TempKey}=C:\\temp\\" };

                CancellationToken cancellationToken = default;

                using (DeployerApp deployerApp = await
                    AppBuilder.BuildAppAsync(args, null, cancellationToken))
                {
                    tempPath = Path.GetTempPath();
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("temp", oldTemp);
            }

            Assert.Equal("C:\\temp\\", tempPath);
        }
    }
}