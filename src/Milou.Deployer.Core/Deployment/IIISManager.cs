using System;

namespace Milou.Deployer.Core.Deployment
{
    public interface IIISManager : IDisposable
    {
        bool StopSiteIfApplicable(DeploymentExecutionDefinition deploymentExecutionDefinition);
    }
}
