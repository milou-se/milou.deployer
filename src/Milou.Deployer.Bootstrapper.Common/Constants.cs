using Arbor.KVConfiguration.Core.Metadata;

namespace Milou.Deployer.Bootstrapper.Common
{
    public static class Constants
    {
        [Metadata]
        public const string PackageId = "Milou.Deployer.ConsoleClient";

        [Metadata]
        public const string DownloadOnly = "--download-only";

        [Metadata]
        public const string AllowPreRelease = "--prerelease";
    }
}