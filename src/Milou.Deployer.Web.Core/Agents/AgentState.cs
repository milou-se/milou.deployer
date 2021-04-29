using System;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentState
    {
        public AgentState(AgentId agentId) => AgentId = agentId;

        public bool IsConnected { get; set; }

        public DateTimeOffset ConnectedAt { get; set; }

        public AgentId AgentId { get; }

        public string? ConnectionId { get; set; }

        public string? CurrentDeploymentTaskId { get; set; }
        public DeploymentTargetId? CurrentDeploymentTargetId { get; set; }
    }
}