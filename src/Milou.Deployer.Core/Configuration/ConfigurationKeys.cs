using Arbor.KVConfiguration.Core.Metadata;

namespace Milou.Deployer.Core.Configuration
{
    public static class ConfigurationKeys
    {
        [Metadata()]
        public const string StopStartIisWebSiteEnabled = "urn:milou-deployer:stop-start-iis-website:enabled";

        [Metadata]
        public const string KeyValueConfigurationFile = "urn:milou-deployer:configuration-file";

        [Metadata]
        public const string SemVer2Normalized = "urn:versioning:semver2:normalized";

        public const string PackageId = "urn:nuget:package-id";

        [Metadata]
        public const string DeployStartTimeUtc = "urn:deployment:start-time-utc";

        [Metadata]
        public const string DeployerEnvironmentConfiguration = "urn:milou-deployer:environment-configuration";

        [Metadata]
        public const string DeployerDeployedFromMachine = "urn:milou-deployer:deployed-from-machine";

        [Metadata]
        public const string DeployerAssemblyVersion = "urn:milou-deployer:deployed-with:assembly-version";

        [Metadata]
        public const string DeployerAssemblyFileVersion = "urn:milou-deployer:deployed-with:assembly-file-version:";

        [Metadata]
        public const string ManifestFileName = "manifest.json";

        [Metadata]
        public const string AllowPreReleaseEnvironmentVariable = "MilouDeployer_AllowPreRelease_Enabled";

        [Metadata]
        public const string LogLevelEnvironmentVariable = "loglevel";

        [Metadata]
        public const string LogLevel = "urn:loglevel";

        [Metadata]
        public const string NuGetExePath = "urn:milou-deployer:tools:nuget:exe-path";

        [Metadata]
        public const string TempDirectory = "urn:milou-deployer:temp";

        [Metadata]
        public const string NuGetSource = "urn:milou-deployer:tools:nuget:source";

        [Metadata]
        public const string ApplicationMetadataFileName = "applicationmetadata.json";

        [Metadata]
        public const string NuGetNoCache = "urn:milou-deployer:tools:nuget:nocache:enabled";

        [Metadata]
        public const string ForceAllowPreRelease = "urn:milou-deployer:environment:allow-pre-release:enabled";

        public static class WebDeploy
        {
            public static class Rules
            {
                [Metadata]
                public const string WhatIfEnabled = "urn:milou-deployer:tools:web-deploy:rules:what-if:enabled";

                [Metadata]
                public const string DoNotDeleteEnabled =
                    "urn:milou-deployer:tools:web-deploy:rules:do-not-delete:enabled";

                [Metadata]
                public const string AppOfflineEnabled =
                    "urn:milou-deployer:tools:web-deploy:rules:app-offline:enabled";

                [Metadata]
                public const string UseChecksumEnabled =
                    "urn:milou-deployer:tools:web-deploy:rules:use-checksum:enabled";

                [Metadata]
                public const string AppDataSkipDirectiveEnabled =
                    "urn:milou-deployer:tools:web-deploy:directives:app-data-skip-directive:enabled";

                [Metadata]
                public const string ApplicationInsightsProfiler2SkipDirectiveEnabled =
                    "urn:milou-deployer:tools:web-deploy:directives:application-insights-profiler-2-directive:enabled";
            }
        }

        [Metadata]
        public const string NuGetConfig = "urn:milou-deployer:tools:nuget:config";
    }
}
