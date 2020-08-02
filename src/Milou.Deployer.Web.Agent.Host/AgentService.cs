using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog;

namespace Milou.Deployer.Web.Agent.Host
{
    public sealed class AgentService : BackgroundService, IAsyncDisposable
    {
        private readonly AgentConfiguration? _agentConfiguration;
        private readonly IDeploymentPackageAgent _deploymentPackageAgent;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;

        private HubConnection? _hubConnection;

        public AgentService(
            IDeploymentPackageAgent deploymentPackageAgent,
            ILogger logger,
            IMediator mediator,
            AgentConfiguration? agentConfiguration = default)
        {
            _deploymentPackageAgent = deploymentPackageAgent;
            _logger = logger;
            _mediator = mediator;
            _agentConfiguration = agentConfiguration;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is { })
            {
                await _hubConnection.StopAsync();

                _logger.Debug("Stopped SignalR in Agent");

                await _hubConnection.DisposeAsync();
            }

            _hubConnection = null;
        }

        private async Task ExecuteDeploymentTask(string deploymentTaskId, string deploymentTargetId)
        {
            if (string.IsNullOrWhiteSpace(deploymentTaskId))
            {
                return;
            }

            var exitCode = await _deploymentPackageAgent.RunAsync(deploymentTaskId, deploymentTargetId);

            var deploymentTaskAgentResult =
                new DeploymentTaskAgentResult(deploymentTaskId, deploymentTargetId, exitCode.IsSuccess);

            await _mediator.Send(deploymentTaskAgentResult);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_agentConfiguration is null)
            {
                _logger.Error("Agent configuration is missing");
                return;
            }

            _logger.Information("Starting Agent service {Service}", nameof(AgentService));

            await Task.Yield();

            AgentId agentId = _agentConfiguration.AgentId();

            if (agentId is null)
            {
                _logger.Error("Could not find agent id, token length is {TokenLength}",
                    _agentConfiguration?.AccessToken.Length.ToString(CultureInfo.InvariantCulture) ?? "N/A");
                return;
            }

            string connectionUrl = $"{_agentConfiguration!.ServerBaseUri}{AgentConstants.HubRoute}";

            CreateSignalRConnection(connectionUrl);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                bool connected = false;

                while (!connected && _hubConnection is {})
                {
                    try
                    {
                        _logger.Debug("Connecting to server via SignalR {Url}", connectionUrl);
                        await _hubConnection.StartAsync(stoppingToken);
                        await _hubConnection.SendAsync("AgentConnect", stoppingToken);
                        connected = true;
                        _logger.Debug("Connected to server");
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        _logger.Error(ex, "Could not connect to server {Url} from agent {Agent}", connectionUrl, agentId);

                        if (_agentConfiguration.StartupDelay >= TimeSpan.FromMilliseconds(20))
                        {
                            await Task.Delay(_agentConfiguration.StartupDelay!.Value, stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not connect to server {Url} from agent {Agent}", connectionUrl, agentId);
            }

            _logger.Debug("Agent background service waiting for cancellation");
            await stoppingToken;
            _logger.Debug("Cancellation requested in Agent app");
            _logger.Debug("Stopping SignalR in Agent");
        }

        private void CreateSignalRConnection(string connectionUrl)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionUrl, options => options.AccessTokenProvider = GetAccessToken)
                .Build();

            _hubConnection.Closed += HubConnectionOnClosed;

            _hubConnection.On<string, string>(AgentConstants.SignalRDeployCommand, ExecuteDeploymentTask);
        }

        private async Task<string> GetAccessToken() => _agentConfiguration!.AccessToken;

        private async Task HubConnectionOnClosed(Exception arg)
        {
            if (_hubConnection is {})
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _hubConnection.StartAsync();
            }
        }
    }
}