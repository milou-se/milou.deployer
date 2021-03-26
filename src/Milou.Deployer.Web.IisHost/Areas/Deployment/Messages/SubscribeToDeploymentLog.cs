using MediatR;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Messages
{
    public class SubscribeToDeploymentLog : IRequest
    {
        public SubscribeToDeploymentLog(string connectionId, DeploymentTargetId deploymentTargetId)
        {
            ConnectionId = connectionId;
            DeploymentTargetId = deploymentTargetId;
        }

        public string ConnectionId { get; }
        public DeploymentTargetId DeploymentTargetId { get; }
    }
}