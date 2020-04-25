using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class RemoteAgentService : IAgentService
    {
        private readonly AgentHub _agentHub;
        private readonly AgentsData _agents;

        public RemoteAgentService(AgentHub agentHub, AgentsData agents)
        {
            _agentHub = agentHub;
            _agents = agents;
        }

        public async Task<IDeploymentPackageAgent> GetAgentForDeploymentTask(
            DeploymentTask deploymentTask,
            CancellationToken cancellationToken)
        {
            if (_agents.Agents.Length == 0)
            {
                throw new InvalidOperationException("No agent available");
            }

            var availableAgents = _agents.Agents.Where(agent => agent.CurrentDeploymentTaskId is null).ToArray();

            if (availableAgents.Length > 0)
            {
                var agentInfo = availableAgents.FirstOrDefault(); // improve algorithm to select agent
                string agentId = agentInfo.Id;
                _agents.AgentAssigned(agentId, deploymentTask.DeploymentTaskId);

                return new RemoteDeploymentPackageAgent(_agentHub, _agents, agentId);
            }

            throw new NotSupportedException("Does not yet support multiple agents");
        }
    }
}