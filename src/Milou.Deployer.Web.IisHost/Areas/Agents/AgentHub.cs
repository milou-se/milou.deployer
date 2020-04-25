using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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

        public override Task OnDisconnectedAsync(Exception exception) =>
            //exception.
            //_mediator.Publish(new AgentDisconnected())
            Task.CompletedTask;

        public override Task OnConnectedAsync()
        {
            _logger.Debug("SignalR Agent client connected, user {User}", Context.User.Identity.Name);

            return base.OnConnectedAsync();
        }

        [PublicAPI]
        public async Task AgentConnect()
        {
            string agentId = Context.UserIdentifier;

            if (string.IsNullOrWhiteSpace(agentId))
            {
                _logger.Warning("The connected agent has no agent id");
                return;
            }

            await _mediator.Publish(new AgentConnected(agentId));
        }
    }
}