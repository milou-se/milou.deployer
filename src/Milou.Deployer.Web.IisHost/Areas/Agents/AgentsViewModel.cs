using System.Collections.Immutable;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentsViewModel
    {
        public AgentsViewModel(ImmutableArray<AgentInfo> agents, ImmutableDictionary<AgentId, string> unknownAgents)
        {
            Agents = agents;
            UnknownAgents = unknownAgents;
        }

        public ImmutableArray<AgentInfo> Agents { get; }
        public ImmutableDictionary<AgentId, string> UnknownAgents { get; }
    }
}