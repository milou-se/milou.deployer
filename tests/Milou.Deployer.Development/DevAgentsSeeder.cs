using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Milou.Deployer.Web.Marten;
using Serilog;

namespace Milou.Deployer.Development
{
    public class DevAgentsSeeder : IDataSeeder
    {
        private readonly DevConfiguration? _devConfiguration;
        private readonly ILogger _logger;
        private readonly IMediator? _mediator;

        public DevAgentsSeeder(ILogger logger, IMediator? mediator = null, DevConfiguration? devConfiguration = null)
        {
            _logger = logger;
            _mediator = mediator;
            _devConfiguration = devConfiguration;
        }

        public int Order { get; } = 1000;

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            if (_mediator is null || _devConfiguration is null)
            {
                return;
            }

            foreach (var agentId in _devConfiguration.Agents.Keys)
            {
                var agentInfo = await _mediator.Send(new GetAgentRequest(agentId), cancellationToken);

                if (agentInfo is null)
                {
                    var createAgentResult = await _mediator.Send(new CreateAgent(agentId), cancellationToken);
                    _devConfiguration.Agents[agentId] =
                        new AgentConfiguration(createAgentResult.AccessToken, _devConfiguration.ServerUrl);
                }
                else
                {
                    var result = await _mediator.Send(new ResetAgentToken(agentId), cancellationToken);

                    _devConfiguration.Agents[agentId] =
                        new AgentConfiguration(result.AccessToken, _devConfiguration.ServerUrl);
                }
            }
        }
    }
}