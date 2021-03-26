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
        private readonly IHostApplicationLifetime _lifetime;

        private HubConnection? _hubConnection;
        private CancellationToken _stoppingToken;
        private string _connectionUrl = "";
        private AgentId? _agentId;

        public AgentService(
            IDeploymentPackageAgent deploymentPackageAgent,
            ILogger logger,
            IMediator mediator,
            IHostApplicationLifetime lifetime,
            AgentConfiguration? agentConfiguration = default)
        {
            _deploymentPackageAgent = deploymentPackageAgent;
            _logger = logger;
            _mediator = mediator;
            _lifetime = lifetime;
            _agentConfiguration = agentConfiguration;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is { })
            {
                _hubConnection.Closed -= HubConnectionOnClosed;

                await _hubConnection.StopAsync(_stoppingToken);

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

            var id = new DeploymentTargetId(deploymentTargetId);

            var exitCode = await _deploymentPackageAgent.RunAsync(deploymentTaskId, id, _stoppingToken);

            var deploymentTaskAgentResult =
                new DeploymentTaskAgentResult(deploymentTaskId, id, exitCode.IsSuccess);

            await _mediator.Send(deploymentTaskAgentResult, _stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;
            if (_agentConfiguration is null)
            {
                _logger.Error("Agent configuration is missing");
                return;
            }

            _logger.Information("Starting Agent service {Service}", nameof(AgentService));

            await Task.Yield();

            _agentId = _agentConfiguration?.AgentId();

            if (_agentId is null)
            {
                _logger.Error("Could not find agent id, token length is {TokenLength}",
                    _agentConfiguration?.AccessToken.Length.ToString(CultureInfo.InvariantCulture) ?? "N/A");
                return;
            }

            _connectionUrl = $"{_agentConfiguration!.ServerBaseUri}{AgentConstants.HubRoute}";

            CreateSignalRConnection(_connectionUrl);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                bool connected = false;

                while (!connected && _hubConnection is {} && !stoppingToken.IsCancellationRequested)
                {
                    connected = await Connect();
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not connect to server {Url} from agent {Agent}", _connectionUrl, _agentId);
            }

            _logger.Debug("Agent background service waiting for cancellation");
            await stoppingToken;
            _logger.Debug("Cancellation requested in Agent app");
            _logger.Debug("Stopping SignalR in Agent");
        }

        private async Task<bool> Connect()
        {
            if (_hubConnection is null || _agentConfiguration is null)
            {
                return false;
            }

            bool connected = false;
            try
            {
                _logger.Debug("Connecting to server via SignalR {Url}", _connectionUrl);
                await _hubConnection.StartAsync(_stoppingToken);
                await _hubConnection.SendAsync("AgentConnect", _stoppingToken);
                connected = true;
                _logger.Debug("Connected to server");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not connect to server {Url} from agent {Agent}", _connectionUrl, _agentId);

                if (_agentConfiguration.StartupDelay >= TimeSpan.FromMilliseconds(20))
                {
                    await Task.Delay(_agentConfiguration.StartupDelay!.Value, _stoppingToken);
                }
            }

            return connected;
        }

        private void CreateSignalRConnection(string connectionUrl)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionUrl, options => options.AccessTokenProvider = GetAccessToken)
                .Build();

            _hubConnection.Closed += HubConnectionOnClosed;

            _hubConnection.On<string, string>(AgentConstants.SignalRDeployCommand, ExecuteDeploymentTask);
            _hubConnection.On("ServerShuttingDown", ShutDown);
        }

        private Task ShutDown()
        {
            if (_stoppingToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            _lifetime.StopApplication();

            return Task.CompletedTask;
        }

        private Task<string> GetAccessToken() => Task.FromResult(_agentConfiguration!.AccessToken);

        private async Task HubConnectionOnClosed(Exception arg)
        {
            if (_stoppingToken.IsCancellationRequested)
            {
                return;
            }

            if (_hubConnection is {})
            {
                await Task.Delay(new Random().Next(0, 5) * 1000, _stoppingToken);
                await _hubConnection.StartAsync(_stoppingToken);
            }
        }
    }
}