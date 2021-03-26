using MediatR;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class EnableTarget : IRequest
    {
        public EnableTarget(string targetId) => TargetId = new DeploymentTargetId(targetId);

        public DeploymentTargetId TargetId { get; }
    }
}