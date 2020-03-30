using System;

namespace Milou.Deployer.Web.Core.Deployment
{
    public static class DeploymentTargetExtensions
    {
        public static string? GetEnvironmentConfiguration(this DeploymentTarget deploymentTarget)
        {
            string? targetEnvironmentConfigName = deploymentTarget.EnvironmentConfiguration;

            return targetEnvironmentConfigName;
        }
    }
}