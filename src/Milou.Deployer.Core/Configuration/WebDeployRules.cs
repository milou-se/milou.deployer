using Arbor.KVConfiguration.Core.Metadata;

namespace Milou.Deployer.Core.Configuration
{
    public static class WebDeployRules
    {
        [Metadata]
        public const string WhatIfEnabled = "urn:milou:deployer:tools:web-deploy:rules:what-if:enabled";

        [Metadata]
        public const string DoNotDeleteEnabled =
            "urn:milou:deployer:tools:web-deploy:rules:do-not-delete:enabled";

        [Metadata]
        public const string AppOfflineEnabled =
            "urn:milou:deployer:tools:web-deploy:rules:app-offline:enabled";

        [Metadata]
        public const string UseChecksumEnabled =
            "urn:milou:deployer:tools:web-deploy:rules:use-checksum:enabled";

        [Metadata]
        public const string AppDataSkipDirectiveEnabled =
            "urn:milou:deployer:tools:web-deploy:directives:app-data-skip-directive:enabled";

        [Metadata]
        public const string ApplicationInsightsProfiler2SkipDirectiveEnabled =
            "urn:milou:deployer:tools:web-deploy:directives:application-insights-profiler-2-directive:enabled";

    }
}