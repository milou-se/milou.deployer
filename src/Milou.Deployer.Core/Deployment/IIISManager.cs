using System;

namespace Milou.Deployer.Core.Deployment
{
    public interface IIISManager : IDisposable
    {
        void StopSiteIfApplicable(DeploymentExecutionDefinition deploymentExecutionDefinition);
    }
}