using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;

using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class FtpHandlerTests
    {
        public FtpHandlerTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        private readonly ITestOutputHelper _testOutputHelper;

        [Fact(Skip = "Depending on publish settings")]
        public async Task PublishFilesShouldSyncFiles()
        {
            var ftpSettings = new FtpSettings(
                new FtpPath("/site/", FileSystemType.Directory),
                publicRootPath: new FtpPath("/site/wwwroot", FileSystemType.Directory));

            var handler = await FtpHandler.CreateWithPublishSettings(
                @"C:\Temp\deploy-test-target.PublishSettings",
                ftpSettings);

            var sourceDirectory = new DirectoryInfo(@"C:\Temp\Ftptest");
            var ruleConfiguration = new RuleConfiguration
            {
                AppOfflineEnabled = true
            };

            using (var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50)))

            {
                var summary = await handler.PublishAsync(ruleConfiguration,
                    sourceDirectory,
                    cancellationTokenSource.Token);

                _testOutputHelper.WriteLine(summary.ToDisplayValue());

                var fileSystemItems = await handler.ListDirectoryAsync(FtpPath.Root, cancellationTokenSource.Token);

                foreach (var fileSystemItem in fileSystemItems)
                {
                    _testOutputHelper.WriteLine(fileSystemItem.Path);
                }
            }
        }
    }
}