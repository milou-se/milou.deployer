using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Arbor.App.Extensions.Time;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Serilog;

namespace Milou.Deployer.Web.Core.Agents
{
    [UsedImplicitly]
    public class AgentsData
    {
        private readonly ConcurrentDictionary<AgentId, AgentState> _agents = new();

        private readonly ICustomClock _customClock;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<AgentId, string> _unknownAgents = new();

        public AgentsData(ICustomClock customClock, ILogger logger)
        {
            _customClock = customClock;
            _logger = logger;
        }

        public ImmutableDictionary<AgentId, string> UnknownAgents => _unknownAgents.ToImmutableDictionary();

        public ImmutableArray<AgentInfo> Agents => _agents
            .Select(agent => new AgentInfo(agent.Key,
                agent.Value.ConnectedAt, agent.Value.ConnectionId, agent.Value.CurrentDeploymentTaskId))
            .ToImmutableArray();

        public void AgentAssigned(AgentId agentId, string deploymentTaskId)
        {
            if (!_agents.TryGetValue(agentId, out var state))
            {
                throw new InvalidOperationException($"The agent {agentId} could not be found");
            }

            state.CurrentDeploymentTaskId = deploymentTaskId;
        }

        public void AgentConnected(AgentConnected agentConnected)
        {
            var agentId = agentConnected.AgentId;

            if (agentId is null)
            {
                throw new InvalidOperationException("Agent connected without agent id");
            }

            if (!_agents.ContainsKey(agentId))
            {
                _agents.TryAdd(agentId,
                    new AgentState(agentId)
                    {
                        ConnectedAt = _customClock.UtcNow(),
                        IsConnected = true,
                        ConnectionId = agentConnected.ConnectionId
                    });
            }
            else
            {
                if (_agents.TryGetValue(agentId, out var state))
                {
                    state.IsConnected = true;
                    state.ConnectedAt = _customClock.UtcNow();
                    state.ConnectionId = agentConnected.ConnectionId;
                }
                else
                {
                    _logger.Error("Could not get agent state for agent id {AgentId}", agentId);
                }
            }
        }

        public void AgentDone(AgentId agentId)
        {
            if (!_agents.TryGetValue(agentId, out var state))
            {
                throw new InvalidOperationException($"The agent {agentId} could not be found");
            }

            state.CurrentDeploymentTaskId = default;
        }

        public void UnknownAgentConnected(UnknownAgentConnected notification) =>
            _unknownAgents.TryAdd(notification.AgentId, notification.ConnectionId);
    }
}