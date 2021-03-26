using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public sealed record AssignTargetToPool
        (AgentPoolId AgentPoolId, DeploymentTargetId DeploymentTargetId) : ICommand<AssignTargetToPoolResult>;
}