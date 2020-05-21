using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.Core.Security;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [Authorize(Policy = AuthorizationPolicies.Agent)]
    [UsedImplicitly]
    public class AgentHub : Hub
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;

        public AgentHub([NotNull] IMediator mediator, ILogger logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await _mediator.Publish(new AgentDisconnected(new AgentId(Context.UserIdentifier)));
        }

        public override Task OnConnectedAsync()
        {
            _logger.Debug("SignalR Agent client connected, user {User}", Context.User.Identity.Name);

            return base.OnConnectedAsync();
        }

        [PublicAPI]
        public async Task AgentConnect()
        {
            if (!AgentId.TryParse(Context.UserIdentifier, out AgentId? agentId))
            {
                _logger.Warning("The connected agent has no agent id");
                return;
            }

            AgentInfo? agentInfo = await _mediator.Send(new GetAgentRequest(agentId));

            if (agentInfo is null)
            {
                _logger.Error("Unknown agent {AgentI} connected", agentId);
                return;
            }

            await _mediator.Publish(new AgentConnected(agentId, Context.ConnectionId));
        }
    }

    public class AgentDisconnected : INotification
    {
        public AgentId AgentId { get; }

        public AgentDisconnected(AgentId agentId)
        {
            AgentId = agentId;
        }
    }
}