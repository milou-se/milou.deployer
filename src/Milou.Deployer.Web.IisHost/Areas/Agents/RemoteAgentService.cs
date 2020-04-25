using System;
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

        public async Task<IDeploymentPackageAgent> GetAgentForDeploymentTask(DeploymentTask deploymentTask,
            CancellationToken cancellationToken)
        {
            if (_agents.Agents.Length == 0)
            {
                throw new InvalidOperationException("No agent available");
            }

            if (_agents.Agents.Length == 1)
            {
                string agentId = _agents.Agents[0].Id;

                return new RemoteDeploymentPackageAgent(_agentHub, agentId);
            }

            throw new NotSupportedException("Does not yet support multiple agents");
        }
    }
}