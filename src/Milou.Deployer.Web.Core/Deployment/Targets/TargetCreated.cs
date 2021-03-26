using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class TargetCreated : IEvent
    {
        public TargetCreated(DeploymentTargetId targetId) => TargetId = targetId;

        public DeploymentTargetId TargetId { get; }
    }
}