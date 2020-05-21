using System.Collections.Generic;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentHistoryResponse : IQueryResult
    {
        public DeploymentHistoryResponse(IReadOnlyCollection<DeploymentTaskInfo> deploymentTasks) =>
            DeploymentTasks = deploymentTasks;

        public IReadOnlyCollection<DeploymentTaskInfo> DeploymentTasks { get; }
    }
}