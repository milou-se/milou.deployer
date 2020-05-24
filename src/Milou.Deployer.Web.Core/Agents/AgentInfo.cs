using System;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentInfo : IQueryResult
    {
        public AgentInfo(AgentId id, DateTimeOffset? connectedAt = null, string? connectionId = null, string? currentDeploymentTaskId = null)
        {
            Id = id;
            ConnectedAt = connectedAt;
            ConnectionId = connectionId;
            CurrentDeploymentTaskId = currentDeploymentTaskId;
        }

        public AgentId Id { get; }

        public DateTimeOffset? ConnectedAt { get; }

        public string? ConnectionId { get; }

        public string? CurrentDeploymentTaskId { get; set; }
    }
}