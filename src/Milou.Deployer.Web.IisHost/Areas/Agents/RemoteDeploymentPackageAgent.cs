using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Microsoft.AspNetCore.SignalR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class RemoteDeploymentPackageAgent : IDeploymentPackageAgent
    {
        private readonly IHubContext<AgentHub> _agentHub;
        private readonly AgentsData _agentsData;
        private readonly ILogger _logger;

        public RemoteDeploymentPackageAgent(IHubContext<AgentHub> agentHub, AgentsData agentsData, AgentId agentId, ILogger logger)
        {
            _agentHub = agentHub;
            _agentsData = agentsData;
            AgentId = agentId;
            _logger = logger;
        }

        public AgentId AgentId { get; }

        public async Task<ExitCode> RunAsync(string deploymentTaskId,
            string deploymentTargetId,
            CancellationToken cancellationToken = default)
        {
            var agent = _agentsData.Agents.SingleOrDefault(current =>
                current.Id.Equals(AgentId));

            if (agent is null)
            {
                _logger.Error("Agents is not found");
                return ExitCode.Failure;
            }

            if (agent?.ConnectionId is null)
            {
                _logger.Error("Agent");
                return ExitCode.Failure;
            }

            await _agentHub.Clients.Clients(agent.ConnectionId).SendAsync(AgentConstants.SignalRDeployCommand,
                deploymentTaskId, deploymentTargetId, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); //TODO

            return ExitCode.Success;
        }
    }
}