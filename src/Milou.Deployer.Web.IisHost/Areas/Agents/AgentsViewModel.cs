using System.Collections.Immutable;
using Milou.Deployer.Web.Core.Agents;

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