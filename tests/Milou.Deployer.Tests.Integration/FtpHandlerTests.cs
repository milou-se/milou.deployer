using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Docker;
using Arbor.Docker.Xunit;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;
using Milou.Deployer.Ftp;
using Milou.Deployer.Tests.Integration.SkipTests;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class FtpHandlerTests : DockerTest
    {
        public FtpHandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper.FromTestOutput())
        {
        }

        protected override async IAsyncEnumerable<ContainerArgs> AddContainersAsync()
        {
            var ftpVariables = new Dictionary<string, string> {["FTP_USER"] = "testuser", ["FTP_PASS"] = "testpw"};

            var passivePorts = new PortRange(21100, 21110);

            var ftpPorts = new List<PortMapping>
            {
                PortMapping.MapSinglePort(30020, 20),
                PortMapping.MapSinglePort(30021, 21),
                new PortMapping(passivePorts, passivePorts)
            };

            var ftp = new ContainerArgs(
                "fauria/vsftpd",
                "ftp",
                ftpPorts,
                ftpVariables
            );

            yield return ftp;
        }

        [Fact(Skip = "Depending on publish settings")]
        public async Task PublishFilesShouldSyncFiles()
        {
            var logger = Context.Logger;

            var (source, deployTargetDirectory, temp) = TestDataHelper.CopyTestData(logger);

            var ftpSettings = new FtpSettings(
                new FtpPath("/", FileSystemType.Directory),
                publicRootPath: new FtpPath("/", FileSystemType.Directory),
                isSecure: false);

            string publishSettingsFile = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src",
                typeof(FtpHandlerTests).Namespace!,
                "ftpdocker.PublishSettings");

            FtpHandler handler = await FtpHandler.CreateWithPublishSettings(
                publishSettingsFile,
                ftpSettings);

            var sourceDirectory = new DirectoryInfo(source);
            var ruleConfiguration = new RuleConfiguration {AppOfflineEnabled = true};

            using var initialCancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50));
            DeploySummary initialSummary = await handler.PublishAsync(ruleConfiguration,
                deployTargetDirectory,
                initialCancellationTokenSource.Token);

            logger.Information("Initial: {Initial}", initialSummary.ToDisplayValue());

            using var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50));
            DeploySummary summary = await handler.PublishAsync(ruleConfiguration,
                sourceDirectory,
                cancellationTokenSource.Token);

            logger.Information("Result: {Result}", summary.ToDisplayValue());

            var fileSystemItems = await handler.ListDirectoryAsync(FtpPath.Root, cancellationTokenSource.Token);

            foreach (FtpPath fileSystemItem in fileSystemItems)
            {
                logger.Information("{Item}", fileSystemItem.Path);
            }

            temp.Dispose();
        }
    }
}