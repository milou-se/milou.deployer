using System;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentInfo
    {
        public AgentInfo(string id, DateTimeOffset connectedAt)
        {
            Id = id;
            ConnectedAt = connectedAt;
        }

        public string Id { get; }
        public DateTimeOffset ConnectedAt { get; }
    }
}