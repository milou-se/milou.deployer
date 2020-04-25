using System.Collections.Immutable;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentsViewModel
    {
        public ImmutableArray<AgentInfo> Agents { get; }

        public AgentsViewModel(ImmutableArray<AgentInfo> agents)
        {
            Agents = agents;
        }

    }
}