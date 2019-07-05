using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class FtpHandlerTests
    {
        public FtpHandlerTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        private readonly ITestOutputHelper _testOutputHelper;

        //[Fact(Skip = "Depending on publish settings")]
        [Fact]
        public async Task PublishFilesShouldSyncFiles()
        {
            var handler =
                await FtpHandler.CreateWithPublishSettings(@"C:\Temp\deploy-test-target.PublishSettings",
                    new FtpSettings(new FtpPath("/site/", FileSystemType.Directory)));
            var sourceDirectory = new DirectoryInfo(@"C:\Temp\Ftptest");
            var ruleConfiguration = new RuleConfiguration();

            using (var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(50)))

            {
                IDeploymentChangeSummary summary = await handler.PublishAsync(ruleConfiguration,
                    sourceDirectory,
                    cancellationTokenSource.Token);

                _testOutputHelper.WriteLine(summary.ToDisplayValue());

                ImmutableArray<FtpPath> fileSystemItems = await handler.ListDirectoryAsync(FtpPath.Root, cancellationToken: cancellationTokenSource.Token);

                foreach (FtpPath fileSystemItem in fileSystemItems)
                {
                    _testOutputHelper.WriteLine(fileSystemItem.Path);
                }
            }
        }
    }
}
