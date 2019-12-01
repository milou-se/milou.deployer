using Microsoft.Web.Deployment;

using Milou.Deployer.Core.Deployment;

namespace Milou.Deployer.Waws
{
    public class ResultAdapter : IDeploymentChangeSummary
    {
        private readonly DeploymentChangeSummary _deploymentChangeSummary;

        public ResultAdapter(DeploymentChangeSummary deploymentChangeSummary) =>
            _deploymentChangeSummary = deploymentChangeSummary;

        public string ToDisplayValue() => _deploymentChangeSummary.ToDisplayValue();
    }
}