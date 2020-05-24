using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentHistoryRequest : IQuery<DeploymentHistoryResponse>
    {
        public DeploymentHistoryRequest(string deploymentTargetId) => DeploymentTargetId = deploymentTargetId;

        public string DeploymentTargetId { get; }
    }
}