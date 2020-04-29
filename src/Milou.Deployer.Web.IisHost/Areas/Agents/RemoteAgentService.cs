using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class RemoteAgentService : IAgentService
    {
        private readonly AgentHub _agentHub;
        private readonly AgentsData _agents;
        private readonly ILogger _logger;

        public RemoteAgentService(AgentHub agentHub, AgentsData agents, ILogger logger)
        {
            _agentHub = agentHub;
            _agents = agents;
            _logger = logger;
        }

        public async Task<IDeploymentPackageAgent> GetAgentForDeploymentTask(
            DeploymentTask deploymentTask,
            CancellationToken cancellationToken)
        {
            if (_agents.Agents.Length == 0)
            {
                throw new InvalidOperationException("No agent available");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var availableAgents = _agents.Agents.Where(agent => agent.CurrentDeploymentTaskId is null).ToArray();

                var agentInfo = availableAgents.FirstOrDefault(); // improve algorithm to select agent

                if (agentInfo is {})
                {
                    _logger.Information("Deployment task {DeploymentTaskId} was assigned to agent {Agent}", deploymentTask.DeploymentTaskId, agentInfo.Id);
                    string agentId = agentInfo.Id;
                    _agents.AgentAssigned(agentId, deploymentTask.DeploymentTaskId);

                    return new RemoteDeploymentPackageAgent(_agentHub, _agents, agentId);
                }

                _logger.Debug("Waiting for agent to be available for deployment task {DeploymentTaskId}", deploymentTask.DeploymentTaskId);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            throw new InvalidOperationException("Waiting for available agent timed out");
        }
    }
}