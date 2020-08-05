using System;
using System.IO;
using Arbor.App.Extensions.Configuration;

namespace Milou.Deployer.Web.Tests.Integration.TestData
{
    public sealed class TestConfiguration : IConfigurationValues, IDisposable
    {
        public TestConfiguration(
            DirectoryInfo baseDirectory,
            FileInfo nugetConfigFile,
            DirectoryInfo siteAppRoot)
        {
            BaseDirectory = baseDirectory;
            NugetConfigFile = nugetConfigFile;
            SiteAppRoot = siteAppRoot;
        }

        public DirectoryInfo BaseDirectory { get; }

        public FileInfo NugetConfigFile { get; }

        public DirectoryInfo SiteAppRoot { get; }
        public string AgentToken { get; set; }

        public void Dispose()
        {
            if (BaseDirectory is {})
            {
                BaseDirectory.Refresh();
                BaseDirectory.Delete(true);
            }
        }

        public override string ToString() =>
            $"{nameof(BaseDirectory)}: {BaseDirectory.FullName}, {nameof(NugetConfigFile)}: {NugetConfigFile.FullName}, {nameof(SiteAppRoot)}: {SiteAppRoot.FullName}";
    }
}