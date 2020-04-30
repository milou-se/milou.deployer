using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;
using Milou.Deployer.Ftp;
using Milou.Deployer.Tests.Integration.SkipTests;
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
            var logger = _testOutputHelper.FromTestOutput();

            var (source, deployTargetDirectory, temp) = TestDataHelper.CopyTestData(logger);

            var ftpSettings = new FtpSettings(
                new FtpPath("/", FileSystemType.Directory),
                publicRootPath: new FtpPath("/", FileSystemType.Directory),
                isSecure: false);

            string publishSettingsFile = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src",
                typeof(FtpHandlerTests).Namespace,
                "ftpdocker.PublishSettings");

            FtpHandler handler = await FtpHandler.CreateWithPublishSettings(
                publishSettingsFile,
                ftpSettings);

            var sourceDirectory = new DirectoryInfo(source);
            var ruleConfiguration = new RuleConfiguration
            {
                AppOfflineEnabled = true
            };

            using var initialCancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50));
            DeploySummary initialSummary = await handler.PublishAsync(ruleConfiguration,
deployTargetDirectory,
initialCancellationTokenSource.Token);

            _testOutputHelper.WriteLine("Initial:");
            _testOutputHelper.WriteLine(initialSummary.ToDisplayValue());

            using var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50));
            DeploySummary summary = await handler.PublishAsync(ruleConfiguration,
sourceDirectory,
cancellationTokenSource.Token);

            _testOutputHelper.WriteLine("Result:");
            _testOutputHelper.WriteLine(summary.ToDisplayValue());

            System.Collections.Immutable.ImmutableArray<FtpPath> fileSystemItems = await handler.ListDirectoryAsync(FtpPath.Root, cancellationTokenSource.Token);

            foreach (FtpPath fileSystemItem in fileSystemItems)
            {
                _testOutputHelper.WriteLine(fileSystemItem.Path);
            }

            temp.Dispose();
        }
    }
}