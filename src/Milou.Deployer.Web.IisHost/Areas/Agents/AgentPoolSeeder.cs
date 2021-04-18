using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentPoolSeeder : IDataSeeder
    {
        private readonly ILogger _logger;
        private readonly IDeploymentTargetReadService _deploymentTargetReadService;
        private readonly IMediator _mediator;

        public AgentPoolSeeder(IMediator mediator, ILogger logger, IDeploymentTargetReadService deploymentTargetReadService)
        {
            _mediator = mediator;
            _logger = logger;
            _deploymentTargetReadService = deploymentTargetReadService;
        }

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            AgentPoolListResult types = await _mediator.Send(new GetAgentPoolsQuery(), cancellationToken);

            if (!types.AgentPools.IsDefaultOrEmpty)
            {
                return;
            }

            var agentPoolId = new AgentPoolId("Default");
            var result = await _mediator.Send(new CreateAgentPool(agentPoolId, new AgentPoolName("Default")), cancellationToken);

            _logger.Debug("CreateAgentPool result for Id {Id}: {Status}", agentPoolId, result);

            var deploymentTargets = await _deploymentTargetReadService.GetDeploymentTargetsAsync(stoppingToken: cancellationToken);

            foreach (var deploymentTarget in deploymentTargets)
            {
                await _mediator.Send(new AssignTargetToPool(agentPoolId, deploymentTarget.Id), cancellationToken);
            }

            var agents = await _mediator.Send(new GetAgentsQuery(), cancellationToken);

            var assignedAgents = await _mediator.Send(new GetAssignedAgentsInPoolsQuery(), cancellationToken);

            foreach (var agent in agents.Agents)
            {
                bool assigned = false;
                foreach (var assignedAgentsAssignedAgent in assignedAgents.AssignedAgents)
                {
                    if (assignedAgentsAssignedAgent.Value.Contains(agent.Id))
                    {
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    await _mediator.Send(new AssignAgentToPool(agentPoolId, agent.Id), cancellationToken);
                }
            }
        }

        public int Order => 100;
    }
}