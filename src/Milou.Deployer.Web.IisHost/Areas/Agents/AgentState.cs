using System;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentState
    {
        public AgentState(string agentId) => AgentId = agentId;

        public bool IsConnected { get; set; }

        public DateTimeOffset ConnectedAt { get; set; }

        public string AgentId { get; }
    }
}