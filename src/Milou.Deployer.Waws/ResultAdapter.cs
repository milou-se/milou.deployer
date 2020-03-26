using System;
using Milou.Deployer.Core.Deployment;

namespace Milou.Deployer.Waws
{
    public class ResultAdapter : IDeploymentChangeSummary
    {
        private readonly WebDeployChangeSummary _deploymentChangeSummary;

        public ResultAdapter(WebDeployChangeSummary deploymentChangeSummary) =>
            _deploymentChangeSummary = deploymentChangeSummary;

        public string ToDisplayValue() => _deploymentChangeSummary.ToDisplayValue();
    }
}