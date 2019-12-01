namespace Milou.Deployer.Core.Deployment.Configuration
{
    public class WebDeployRulesConfig
    {
        public WebDeployRulesConfig(
            bool doNotDeleteRuleEnabled,
            bool appOfflineRuleEnabled,
            bool useChecksumRuleEnabled,
            bool appDataSkipDirectiveEnabled,
            bool applicationInsightsProfiler2SkipDirectiveEnabled)
        {
            DoNotDeleteRuleEnabled = doNotDeleteRuleEnabled;
            AppOfflineRuleEnabled = appOfflineRuleEnabled;
            UseChecksumRuleEnabled = useChecksumRuleEnabled;
            AppDataSkipDirectiveEnabled = appDataSkipDirectiveEnabled;
            ApplicationInsightsProfiler2SkipDirectiveEnabled = applicationInsightsProfiler2SkipDirectiveEnabled;
        }

        public bool DoNotDeleteRuleEnabled { get; }

        public bool AppOfflineRuleEnabled { get; }

        public bool UseChecksumRuleEnabled { get; }

        public bool AppDataSkipDirectiveEnabled { get; }

        public bool ApplicationInsightsProfiler2SkipDirectiveEnabled { get; }
    }
}
