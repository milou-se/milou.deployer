using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentsQueryResult : IQueryResult
    {
        public ImmutableArray<AgentInfo> Agents { get; }

        public AgentsQueryResult(ImmutableArray<AgentInfo> agents)
        {
            Agents = agents;
        }
    }
}