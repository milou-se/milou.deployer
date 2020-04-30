using System;
using System.Collections.Immutable;
using System.IO;
using Milou.Deployer.Core.IO;
using Serilog;
using Xunit;

namespace Milou.Deployer.Tests.Integration.SkipTests
{
    internal static class TestDataHelper
    {
        public static (string source, DirectoryInfo deployTargetDirectory, TempDirectory tempTargetDir) CopyTestData(ILogger logger)
        {

            string testDataPath = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src",
                "Milou.Deployer.Tests.Integration", "TestData", "AppDataTest");
            string source = Path.Combine(testDataPath, "Source");
            string target = Path.Combine(testDataPath, "Target");

            var tempTargetDir = TempDirectory.CreateTempDirectory();
            var deployTargetDirectory = tempTargetDir.Directory;

            deployTargetDirectory.Refresh();
            RecursiveIO.RecursiveDelete(deployTargetDirectory, logger);

            deployTargetDirectory.EnsureExists();
            var testTargetDirectory = new DirectoryInfo(target);
            RecursiveIO.RecursiveCopy(testTargetDirectory, deployTargetDirectory, logger,
                ImmutableArray<string>.Empty);

            deployTargetDirectory.Refresh();

            var filesBefore = deployTargetDirectory.GetFiles();

            foreach (var fileInfo in filesBefore)
            {
                logger.Debug("Existing file before deploy: {File}", fileInfo.Name);
            }

            Assert.Contains(filesBefore, file => file.Name.Equals("DeleteMe.txt", StringComparison.OrdinalIgnoreCase));

            return (source,deployTargetDirectory,tempTargetDir);
        }
    }
}