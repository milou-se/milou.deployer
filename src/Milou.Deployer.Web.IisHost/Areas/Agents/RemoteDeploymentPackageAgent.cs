using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Microsoft.AspNetCore.SignalR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class RemoteDeploymentPackageAgent : IDeploymentPackageAgent
    {
        private readonly AgentHub _agentHub;
        private readonly AgentsData _agentsData;

        public RemoteDeploymentPackageAgent(AgentHub agentHub, AgentsData agentsData, AgentId agentId)
        {
            _agentHub = agentHub;
            _agentsData = agentsData;
            AgentId = agentId;
        }

        public AgentId AgentId { get; }

        public async Task<ExitCode> RunAsync(string deploymentTaskId,
            string deploymentTargetId,
            CancellationToken cancellationToken = default)
        {
            var agent = _agentsData.Agents.SingleOrDefault(current =>
                current.Id.Equals(AgentId));

            await _agentHub.Clients.Clients(agent.ConnectionId).SendAsync(AgentConstants.SignalRDeployCommand,
                deploymentTaskId, deploymentTargetId, cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); //TODO

            return ExitCode.Success;
        }
    }
}