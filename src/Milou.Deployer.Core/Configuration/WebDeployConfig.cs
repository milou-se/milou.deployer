namespace Milou.Deployer.Core.Configuration
{
    public class WebDeployConfig
    {
        public WebDeployRulesConfig Rules { get; }

        public WebDeployConfig(WebDeployRulesConfig rules)
        {
            Rules = rules;
        }
    }
}