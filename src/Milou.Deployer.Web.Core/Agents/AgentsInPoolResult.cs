﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentsInPoolResult : IQueryResult
    {
        public ImmutableArray<AgentId> Agents { get; }

        public AgentsInPoolResult(IReadOnlyCollection<AgentId> agentIds) => Agents = agentIds.ToImmutableArray();
    }
}