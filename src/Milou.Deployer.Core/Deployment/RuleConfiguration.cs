using System.Collections.Generic;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment.Configuration;

using Serilog;

namespace Milou.Deployer.Core.Deployment
{
    public class RuleConfiguration
    {
        private static readonly List<string> DefaultExcludes = new List<string>
        {
            "/locks",
            "/deployments",
            "/site/locks/",
            "/site/deployments/",
        };

        public List<string> Excludes { get; private set; } = DefaultExcludes;

        public bool AppOfflineEnabled { get; set; } = true;

        public bool DoNotDeleteEnabled { get; set; }

        public bool WhatIfEnabled { get; set; }

        public bool ApplicationInsightsProfiler2SkipDirectiveEnabled { get; set; }

        public bool AppDataSkipDirectiveEnabled { get; set; } = true;

        public bool UseChecksumEnabled { get; set; }

        public static RuleConfiguration Get(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DeployerConfiguration deployerConfiguration,
            ILogger logger)
        {
            bool doNotDeleteEnabled = deploymentExecutionDefinition.DoNotDeleteEnabled(deployerConfiguration
                .WebDeploy.Rules.DoNotDeleteRuleEnabled);

            bool useChecksumEnabled = deploymentExecutionDefinition.UseChecksumEnabled(deployerConfiguration
                .WebDeploy.Rules.UseChecksumRuleEnabled);

            bool appDataSkipDirectiveEnabled = deploymentExecutionDefinition.AppDataSkipDirectiveEnabled(
                deployerConfiguration
                    .WebDeploy.Rules.AppDataSkipDirectiveEnabled);

            bool applicationInsightsProfiler2SkipDirectiveEnabled =
                deploymentExecutionDefinition.ApplicationInsightsProfiler2SkipDirectiveEnabled(
                    deployerConfiguration
                        .WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled);

            bool appOfflineEnabled = deploymentExecutionDefinition.AppOfflineEnabled(deployerConfiguration
                .WebDeploy.Rules.AppOfflineRuleEnabled);

            bool whatIfEnabled = deploymentExecutionDefinition.WhatIfEnabled();

            logger.Debug(
                "{RuleName}: {DoNotDeleteEnabled}",
                nameof(deployerConfiguration.WebDeploy.Rules.DoNotDeleteRuleEnabled),
                doNotDeleteEnabled);
            logger.Debug(
                "{RuleName}: {AppOfflineEnabled}",
                nameof(deployerConfiguration.WebDeploy.Rules.AppOfflineRuleEnabled),
                appOfflineEnabled);
            logger.Debug(
                "{RuleName}: {UseChecksumEnabled}",
                nameof(deployerConfiguration.WebDeploy.Rules.UseChecksumRuleEnabled),
                useChecksumEnabled);
            logger.Debug(
                "{RuleName}: {AppDataSkipDirectiveEnabled}",
                nameof(deployerConfiguration.WebDeploy.Rules.AppDataSkipDirectiveEnabled),
                appDataSkipDirectiveEnabled);
            logger.Debug(
                "{RuleName}: {ApplicationInsightsProfiler2SkipDirectiveEnabled}",
                nameof(deployerConfiguration.WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled),
                applicationInsightsProfiler2SkipDirectiveEnabled);
            logger.Debug(
                "{RuleName}: {WhatIfEnabled}",
                nameof(DeploymentExecutionDefinitionExtensions.WhatIfEnabled),
                whatIfEnabled);

            return new RuleConfiguration
            {
                UseChecksumEnabled = useChecksumEnabled,
                AppDataSkipDirectiveEnabled = appDataSkipDirectiveEnabled,
                ApplicationInsightsProfiler2SkipDirectiveEnabled = applicationInsightsProfiler2SkipDirectiveEnabled,
                WhatIfEnabled = whatIfEnabled,
                DoNotDeleteEnabled = doNotDeleteEnabled,
                AppOfflineEnabled = appOfflineEnabled,
                Excludes = DefaultExcludes
            };
        }
    }
}
