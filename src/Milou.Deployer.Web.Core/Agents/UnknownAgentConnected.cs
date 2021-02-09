﻿using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class UnknownAgentConnected : IEvent
    {
        public UnknownAgentConnected(AgentId agentId, string connectionId)
        {
            ConnectionId = connectionId;
            AgentId = agentId;
        }

        public string ConnectionId { get; }

        public AgentId AgentId { get; }
    }
}