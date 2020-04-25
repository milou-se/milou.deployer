using System;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentInfo
    {
        public AgentInfo(string id, DateTimeOffset connectedAt, string? connectionId, string? currentDeploymentTaskId)
        {
            Id = id;
            ConnectedAt = connectedAt;
            ConnectionId = connectionId;
            CurrentDeploymentTaskId = currentDeploymentTaskId;
        }

        public string Id { get; }

        public DateTimeOffset ConnectedAt { get; }

        public string? ConnectionId { get; }

        public string? CurrentDeploymentTaskId { get; set; }
    }
}