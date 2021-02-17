﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Milou.Deployer.Web.Tests.Integration.TestData
{
    public static class TestPathHelper
    {
        public static async Task<TestConfiguration> CreateTestConfigurationAsync(CancellationToken cancellationToken)
        {
            const string projectName = "Milou.Deployer.Web.Tests.Integration";

            string baseDirectoryPath = Path.Combine(Path.GetTempPath(),
                projectName + "-" + DateTime.UtcNow.Ticks);

            var baseDirectory = new DirectoryInfo(baseDirectoryPath);

            baseDirectory.Create();
            DirectoryInfo targetAppRoot = baseDirectory.CreateSubdirectory("target");
            DirectoryInfo nugetBaseDirectory = baseDirectory.CreateSubdirectory("nuget");
            DirectoryInfo nugetPackageDirectory = nugetBaseDirectory.CreateSubdirectory("packages");

            var nugetConfigFile = new FileInfo(Path.Combine(VcsTestPathHelper.GetRootDirectory(), "tests",
                "Milou.Deployer.Web.Tests.Integration", "TestData", "nuget.config"));

            var testConfiguration = new TestConfiguration(baseDirectory,
                nugetConfigFile,
                targetAppRoot);

            string nugetConfigContent = await File.ReadAllTextAsync(nugetConfigFile.FullName, cancellationToken);

            Console.WriteLine(
                $"Created test configuration {testConfiguration} with nuget config file content {Environment.NewLine}{nugetConfigContent}");

            return testConfiguration;
        }
    }
}