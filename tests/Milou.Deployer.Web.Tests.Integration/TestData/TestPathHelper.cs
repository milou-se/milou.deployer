using System;
using System.IO;
using System.Text;
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
            var packagesDirectory = nugetBaseDirectory.CreateSubdirectory("packages");

            var nugetConfigFile = new FileInfo(Path.Combine(VcsTestPathHelper.GetRootDirectory(), "tests",
                "Milou.Deployer.Web.Tests.Integration", "TestData", "nuget.config"));

            if (!nugetConfigFile.Exists)
            {
                throw new InvalidOperationException($"The nuget config file {nugetConfigFile.FullName} does not exist");
            }

            var packages = new DirectoryInfo(Path.Combine(VcsTestPathHelper.GetRootDirectory(), "tests",
                "Milou.Deployer.Web.Tests.Integration", "TestData", "packages")).GetFiles("*.nupkg");


            string testNuGetConfig = Path.Combine(nugetBaseDirectory.FullName, "nuget.config");

            string nugetConfigContent = await File.ReadAllTextAsync(nugetConfigFile.FullName, cancellationToken);

            string testConfigContent = nugetConfigContent.Replace("Packages", packagesDirectory.FullName);

            await File.WriteAllTextAsync(testNuGetConfig, testConfigContent, Encoding.UTF8, cancellationToken);

            foreach (var fileInfo in packages)
            {
                fileInfo.CopyTo(Path.Combine(packagesDirectory.FullName, fileInfo.Name), overwrite: true);
            }

            var testConfiguration = new TestConfiguration(baseDirectory,
                new FileInfo(testNuGetConfig),
                targetAppRoot);

            Console.WriteLine(
                $"Created test configuration {testConfiguration} with nuget config file content {Environment.NewLine}{nugetConfigContent}");

            return testConfiguration;
        }
    }
}