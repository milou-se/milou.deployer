using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
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
            var result = await _mediator.Send(new CreateAgentPool(agentPoolId), cancellationToken);

            _logger.Debug("CreateAgentPool result for Id {Id}: {Status}", agentPoolId, result);

            var deploymentTargets = await _deploymentTargetReadService.GetDeploymentTargetsAsync(stoppingToken: cancellationToken);

            foreach (var deploymentTarget in deploymentTargets)
            {
                await _mediator.Send(new AssignTargetToPool(agentPoolId, new DeploymentTargetId(deploymentTarget.Id)), cancellationToken);
            }
        }

        public int Order => 100;
    }
}