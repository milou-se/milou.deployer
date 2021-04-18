using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class TargetEnabled : IEvent
    {
        public TargetEnabled(DeploymentTargetId targetId) => TargetId = targetId;

        public DeploymentTargetId TargetId { get; }
    }
}