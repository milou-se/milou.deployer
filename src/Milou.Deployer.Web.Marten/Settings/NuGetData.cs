using System;

namespace Milou.Deployer.Web.Marten.Settings
{
    [MartenData]
    public record NuGetData
    {
        public TimeSpan? PackageListTimeout { get; set; }

        public string? NuGetConfigFile { get; set; }

        public string? NuGetPackageSource { get; set; }
    }
}