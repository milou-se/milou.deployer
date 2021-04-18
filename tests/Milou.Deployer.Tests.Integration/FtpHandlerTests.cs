﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Docker;
using Arbor.Docker.Xunit;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;
using Milou.Deployer.Ftp;
using Milou.Deployer.Tests.Integration.SkipTests;
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
            var passivePorts = new PortRange(21100, 21110);

            var ftpVariables = new Dictionary<string, string>
            {
                ["FTP_USER"] = "testuser",
                ["FTP_PASS"] = "testpw",
                ["PASV_MIN_PORT"] = passivePorts.Start.ToString(),
                ["PASV_MAX_PORT"] = passivePorts.End.ToString()
            };

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

        [ConditionalFactAttribute]
        public async Task PublishFilesShouldSyncFiles()
        {
            var logger = Context.Logger;

            var (source, deployTargetDirectory, temp) = TestDataHelper.CopyTestData(logger);

            var ftpSettings = new FtpSettings(
                new FtpPath("/", FileSystemType.Directory),
                publicRootPath: new FtpPath("/", FileSystemType.Directory),
                isSecure: false);

            FtpHandler handler = await FtpHandler.Create(new Uri("ftp://127.0.0.1:30021"),
                ftpSettings, new NetworkCredential("testuser", "testpw"), logger);

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