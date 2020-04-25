﻿using MediatR;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentConnected : INotification
    {
        public AgentConnected(string agentId) => AgentId = agentId;

        public string AgentId { get; }
    }
}