using System.Collections.Generic;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host.Configuration;

namespace Milou.Deployer.Development
{
    public class DevConfiguration
    {
        public string ServerUrl { get; set; } = "http://localhost:34343";

        public Dictionary<AgentId, AgentConfiguration?> Agents { get; } = new();
        public KeyData Key { get; init; }
    }
}