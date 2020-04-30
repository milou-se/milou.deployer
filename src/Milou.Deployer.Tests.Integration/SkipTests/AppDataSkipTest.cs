using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milou.Deployer.Core.IO;
using Milou.Deployer.Waws;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration.SkipTests
{
    public class AppDataSkipTest
    {
        private readonly ITestOutputHelper _output;

        public AppDataSkipTest(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task ShouldSkipAppData()
        {
            string testDataPath = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src",
                "Milou.Deployer.Tests.Integration", "TestData", "AppDataTest");
            string source = Path.Combine(testDataPath, "Source");
            string target = Path.Combine(testDataPath, "Target");

            var logger = _output.FromTestOutput();

            using var tempTargetDir = TempDirectory.CreateTempDirectory();
            var deployTargetDirectory = tempTargetDir.Directory;

            deployTargetDirectory.Refresh();
            RecursiveIO.RecursiveDelete(deployTargetDirectory, logger);

            deployTargetDirectory.EnsureExists();
            var testTargetDirectory = new DirectoryInfo(target);
            RecursiveIO.RecursiveCopy(testTargetDirectory, deployTargetDirectory, logger,
                ImmutableArray<string>.Empty);

            deployTargetDirectory.Refresh();

            var webDeployHelper = new WebDeployHelper(logger);

            var filesBefore = deployTargetDirectory.GetFiles();

            foreach (var fileInfo in filesBefore)
            {
                logger.Debug("Existing file before deploy: {File}", fileInfo.Name);
            }

            Assert.Contains(filesBefore, file => file.Name.Equals("DeleteMe.txt", StringComparison.OrdinalIgnoreCase));

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

            Assert.DoesNotContain(filesAfter, file => file.Name.Equals("DeleteMe.txt", StringComparison.OrdinalIgnoreCase));

            Assert.Contains("AddMe.txt", result.CreatedFiles.Select(f => new FileInfo(f).Name));
            Assert.Contains("UpdateMe.txt", result.UpdatedFiles.Select(f => new FileInfo(f).Name));
            Assert.Contains("DeleteMe.txt", result.DeletedFiles.Select(f => new FileInfo(f).Name));

            var appDataFileContent = await
                File.ReadAllTextAsync(Path.Combine(deployTargetDirectory.FullName, "App_Data", "Data.txt"));

            Assert.Equal("Defined in target", appDataFileContent);
        }
    }
}