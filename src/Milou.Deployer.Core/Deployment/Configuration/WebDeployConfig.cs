namespace Milou.Deployer.Core.Deployment.Configuration
{
    public class WebDeployConfig
    {
        public WebDeployConfig(WebDeployRulesConfig rules) => Rules = rules;

        public WebDeployRulesConfig Rules { get; }
    }
}