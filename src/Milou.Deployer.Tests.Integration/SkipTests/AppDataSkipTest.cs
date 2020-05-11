using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milou.Deployer.Waws;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration.SkipTests
{
    public class AppDataSkipTest
    {
        public AppDataSkipTest(ITestOutputHelper output) => _output = output;
        private readonly ITestOutputHelper _output;

        [Fact]
        public async Task ShouldSkipAppData()
        {
            var logger = _output.FromTestOutput();
            var webDeployHelper = new WebDeployHelper(logger);

            var (source, deployTargetDirectory, temp) = TestDataHelper.CopyTestData(logger);


            var result = await webDeployHelper.DeployContentToOneSiteAsync(
                source, null, TimeSpan.MinValue,
                appDataSkipDirectiveEnabled: true,
                doNotDelete: false,
                logAction: message => logger.Information("{Message}", message),
                targetPath: deployTargetDirectory.FullName);

            logger.Information("Result: {Result}", result.ToDisplayValue());

            deployTargetDirectory.Refresh();

            var filesAfter = deployTargetDirectory.GetFiles();

            foreach (var fileInfo in filesAfter)
            {
                logger.Debug("Existing file after deploy: {File}", fileInfo.Name);
            }

            Assert.DoesNotContain(filesAfter,
                file => file.Name.Equals("DeleteMe.txt", StringComparison.OrdinalIgnoreCase));

            Assert.Contains("AddMe.txt", result.CreatedFiles.Select(f => new FileInfo(f).Name));
            Assert.Contains("UpdateMe.txt", result.UpdatedFiles.Select(f => new FileInfo(f).Name));
            Assert.Contains("DeleteMe.txt", result.DeletedFiles.Select(f => new FileInfo(f).Name));

            var appDataFileContent = await
                File.ReadAllTextAsync(Path.Combine(deployTargetDirectory.FullName, "App_Data", "Data.txt"));

            Assert.Equal("Defined in target", appDataFileContent);

            temp.Dispose();
        }
    }
}