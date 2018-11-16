using Milou.Deployer.Core.Configuration;
using Serilog;

namespace Milou.Deployer.Core.Deployment
{
    public class RuleConfiguration
    {
        public static RuleConfiguration Get(DeploymentExecutionDefinition deploymentExecutionDefinition, DeployerConfiguration DeployerConfiguration, ILogger _logger)
        {

            bool doNotDeleteEnabled = deploymentExecutionDefinition.DoNotDeleteEnabled(DeployerConfiguration
                .WebDeploy.Rules.DoNotDeleteRuleEnabled);

            bool useChecksumEnabled = deploymentExecutionDefinition.UseChecksumEnabled(DeployerConfiguration
                .WebDeploy.Rules.UseChecksumRuleEnabled);

            bool appDataSkipDirectiveEnabled = deploymentExecutionDefinition.AppDataSkipDirectiveEnabled(
                DeployerConfiguration
                    .WebDeploy.Rules.AppDataSkipDirectiveEnabled);

            bool applicationInsightsProfiler2SkipDirectiveEnabled =
                deploymentExecutionDefinition.ApplicationInsightsProfiler2SkipDirectiveEnabled(
                    DeployerConfiguration
                        .WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled);

            bool appOfflineEnabled = deploymentExecutionDefinition.AppOfflineEnabled(DeployerConfiguration
                .WebDeploy.Rules.AppOfflineRuleEnabled);

            bool whatIfEnabled = deploymentExecutionDefinition.WhatIfEnabled(false);

            _logger.Debug("{RuleName}: {DoNotDeleteEnabled}",
                nameof(DeployerConfiguration.WebDeploy.Rules.DoNotDeleteRuleEnabled),
                doNotDeleteEnabled);
            _logger.Debug("{RuleName}: {AppOfflineEnabled}",
                nameof(DeployerConfiguration.WebDeploy.Rules.AppOfflineRuleEnabled),
                appOfflineEnabled);
            _logger.Debug("{RuleName}: {UseChecksumEnabled}",
                nameof(DeployerConfiguration.WebDeploy.Rules.UseChecksumRuleEnabled),
                useChecksumEnabled);
            _logger.Debug("{RuleName}: {AppDataSkipDirectiveEnabled}",
                nameof(DeployerConfiguration.WebDeploy.Rules.AppDataSkipDirectiveEnabled),
                appDataSkipDirectiveEnabled);
            _logger.Debug("{RuleName}: {ApplicationInsightsProfiler2SkipDirectiveEnabled}",
                nameof(DeployerConfiguration.WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled),
                applicationInsightsProfiler2SkipDirectiveEnabled);
            _logger.Debug("{RuleName}: {WhatIfEnabled}",
                nameof(DeploymentExecutionDefinitionExtensions.WhatIfEnabled),
                whatIfEnabled);

            return new RuleConfiguration
            {
                UseChecksumEnabled = useChecksumEnabled,
                AppDataSkipDirectiveEnabled = appDataSkipDirectiveEnabled,
                ApplicationInsightsProfiler2SkipDirectiveEnabled = applicationInsightsProfiler2SkipDirectiveEnabled,
                WhatIfEnabled = whatIfEnabled,
                DoNotDeleteEnabled = doNotDeleteEnabled,
                AppOfflineEnabled = appOfflineEnabled
            };
        }

        public bool AppOfflineEnabled { get; set; }

        public bool DoNotDeleteEnabled { get; set; }

        public bool WhatIfEnabled { get; set; }

        public bool ApplicationInsightsProfiler2SkipDirectiveEnabled { get; set; }

        public bool AppDataSkipDirectiveEnabled { get; set; }

        public bool UseChecksumEnabled { get; set; }
    }
}