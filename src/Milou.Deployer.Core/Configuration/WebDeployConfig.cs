namespace Milou.Deployer.Core.Configuration
{
    public class WebDeployConfig
    {
        public WebDeployConfig(WebDeployRulesConfig rules)
        {
            Rules = rules;
        }

        public WebDeployRulesConfig Rules { get; }
    }
}
