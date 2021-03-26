using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Tasks;
using Arbor.App.Extensions.Time;
using Arbor.KVConfiguration.Urns;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Services
{
    public sealed class DeploymentWorkerService : BackgroundService,
        INotificationHandler<WorkerCreated>,
        INotificationHandler<TargetEnabled>,
        INotificationHandler<TargetDisabled>,
        INotificationHandler<AgentLogNotification>,
        IRequestHandler<StartWorker>,
        INotificationHandler<AgentDeploymentDone>,
        INotificationHandler<AgentDeploymentFailed>,
        IAsyncDisposable
    {
        private readonly AgentsData _agents;
        private readonly Dictionary<DeploymentTargetId, CancellationTokenSource> _cancellations;
        private readonly ConfigurationInstanceHolder _configurationInstanceHolder;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly Dictionary<DeploymentTargetId, Task> _tasks;
        private readonly TimeoutHelper _timeoutHelper;
        private List<DeploymentTargetWorker> _workers;
        private CancellationToken _stoppingToken;
        private bool _isDisposing;
        private bool _isDisposed;

        public DeploymentWorkerService(
            ConfigurationInstanceHolder configurationInstanceHolder,
            ILogger logger,
            IMediator mediator,
            AgentsData agents,
            TimeoutHelper timeoutHelper)
        {
            _configurationInstanceHolder = configurationInstanceHolder;
            _logger = logger;
            _mediator = mediator;
            _agents = agents;
            _timeoutHelper = timeoutHelper;
            _tasks = new();
            _cancellations = new ();
        }

        public Task Handle(AgentDeploymentDone notification, CancellationToken cancellationToken)
        {
            var workerByTargetId = GetWorkerByTargetId(notification.DeploymentTargetId);

            workerByTargetId?.NotifyDeploymentDone(notification);

            _agents.AgentDone(notification.AgentId);

            return Task.CompletedTask;
        }

        public Task Handle(AgentDeploymentFailed notification, CancellationToken cancellationToken)
        {
            var workerByTargetId = GetWorkerByTargetId(notification.DeploymentTargetId);

            workerByTargetId?.NotifyDeploymentFailed(notification);

            _agents.AgentDone(notification.AgentId);

            return Task.CompletedTask;
        }

        public Task Handle(AgentLogNotification notification, CancellationToken cancellationToken)
        {
            var workerByTargetId = GetWorkerByTargetId(notification.DeploymentTargetId);

            workerByTargetId?.LogProgress(notification);

            return Task.CompletedTask;
        }

        public async Task Handle(TargetDisabled notification, CancellationToken cancellationToken)
        {
            if (!_configurationInstanceHolder.TryGet(notification.TargetId,
                out DeploymentTargetWorker? worker))
            {
                _logger.Warning("Could not get worker for target id {TargetId}", notification.TargetId);
                return;
            }

            await StopWorkerAsync(worker, cancellationToken);
        }

        public Task Handle(TargetEnabled notification, CancellationToken cancellationToken)
        {
            if (!_configurationInstanceHolder.TryGet(notification.TargetId.TargetId,
                out DeploymentTargetWorker? worker))
            {
                _logger.Warning("Could not get worker for target id {TargetId}", notification.TargetId);
                return Task.CompletedTask;
            }

            StartWorker(worker!, cancellationToken);

            return Task.CompletedTask;
        }

        public Task Handle(WorkerCreated notification, CancellationToken cancellationToken)
        {
            if (!_configurationInstanceHolder.TryGet(
                notification.Worker.TargetId.TargetId,
                out DeploymentTargetWorker _))
            {
                _configurationInstanceHolder.Add(
                    new NamedInstance<IDeploymentTargetWorker>(notification.Worker, notification.Worker.TargetId.TargetId));
            }

            StartWorker(notification.Worker, cancellationToken);

            return Task.CompletedTask;
        }

        public Task<Unit> Handle(StartWorker request, CancellationToken cancellationToken)
        {
            StartWorker(request.Worker, cancellationToken);

            return Task.FromResult(Unit.Value);
        }

        private DeploymentTargetWorker? GetWorkerByTargetId([NotNull] DeploymentTargetId targetId)
        {
            if (!_configurationInstanceHolder.TryGet(targetId.TargetId,
                out DeploymentTargetWorker? worker))
            {
                int registered = _configurationInstanceHolder.RegisteredTypes
                    .Count(type => type == typeof(DeploymentTargetWorker));

                _logger.Warning("Could not get worker for target id {TargetId}, {Count} worker types registered",
                    targetId, registered);
            }

            return worker;
        }

        public async Task Enqueue([NotNull] DeploymentTask deploymentTask)
        {
            if (_stoppingToken.IsCancellationRequested)
            {
                _logger.Warning("Cancellation is request, deployment task is not enqueued");
                return;
            }

            var foundWorker = GetWorkerByTargetId(deploymentTask.DeploymentTargetId);

            if (foundWorker is null)
            {
                _logger.Error("Could not find worker for deployment target id {DeploymentTargetId}",
                    deploymentTask.DeploymentTargetId);
                return;
            }

            bool enqueued = foundWorker.Enqueue(deploymentTask);

            if (enqueued)
            {
                await _mediator.Publish(new DeploymentTaskCreated(deploymentTask), _stoppingToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            stoppingToken.Register(() =>
            {
                foreach (var cancellationTokenSource in _cancellations)
                {
                    if (!cancellationTokenSource.Value.IsCancellationRequested)
                    {
                        cancellationTokenSource.Value.Cancel();
                    }
                }
            });

            await Task.Yield();

            _workers = _configurationInstanceHolder.GetInstances<DeploymentTargetWorker>().Values
                .NotNull()
                .ToList();

            foreach (var deploymentTargetWorker in _workers)
            {
                StartWorker(deploymentTargetWorker, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var completedTaskKeys = _tasks
                        .Where(pair => pair.Value.IsCompletedSuccessfully)
                        .Select(pair => pair.Key)
                        .ToArray();

                    foreach (var completedTaskKey in completedTaskKeys)
                    {
                        if (_tasks.ContainsKey(completedTaskKey))
                        {
                            _tasks.Remove(completedTaskKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Could not clean up completed worker tasks");
                }

                await stoppingToken;
            }

            await Task.WhenAll(_tasks.Values.Where(task => !task.IsCompleted));
        }

        private void StartWorker(IDeploymentTargetWorker deploymentTargetWorker, CancellationToken stoppingToken)
        {
            try
            {
                TryStartWorker(deploymentTargetWorker, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not start worker for target id {TargetId}", deploymentTargetWorker.TargetId);
            }
        }

        private void TryStartWorker(IDeploymentTargetWorker deploymentTargetWorker, CancellationToken stoppingToken)
        {
            _logger.Debug("Trying to start worker for target id {TargetId}", deploymentTargetWorker.TargetId);

            if (_tasks.ContainsKey(deploymentTargetWorker.TargetId))
            {
                if (deploymentTargetWorker.IsRunning)
                {
                    _logger.Debug("Worker for target id {TargetId} is already running",
                        deploymentTargetWorker.TargetId);
                    return;
                }

                Task task = _tasks[deploymentTargetWorker.TargetId];

                if (!task.IsCompleted && _cancellations.ContainsKey(deploymentTargetWorker.TargetId))
                {
                    CancellationTokenSource tokenSource = _cancellations[deploymentTargetWorker.TargetId];

                    try
                    {
                        if (!tokenSource.IsCancellationRequested)
                        {
                            tokenSource.Cancel();
                        }

                        tokenSource.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignore
                    }

                    _cancellations.Remove(deploymentTargetWorker.TargetId);
                }

                _tasks.Remove(deploymentTargetWorker.TargetId);
            }
            else
            {
                _logger.Debug("Start worker task was not found for target id {TargetId}",
                    deploymentTargetWorker.TargetId);
            }

            var cancellationTokenSource = _timeoutHelper.CreateCancellationTokenSource();

            var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancellationTokenSource.Token);

            _cancellations.TryAdd(deploymentTargetWorker.TargetId, cancellationTokenSource);

            _tasks.Add(deploymentTargetWorker.TargetId,
                Task.Run(() => deploymentTargetWorker.ExecuteAsync(linked.Token), linked.Token));
        }

        private async Task StopWorkerAsync(DeploymentTargetWorker? worker, CancellationToken cancellationToken)
        {
            if (worker is { })
            {
                _logger.Debug("Stopping worker for target id {TargetId}", worker.TargetId);
                await worker.StopAsync(cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposing || _isDisposed)
            {
                return;
            }

            _isDisposing = true;

            foreach (var deploymentTargetWorker in _workers)
            {
                await deploymentTargetWorker.StopAsync(CancellationToken.None);
            }

            foreach (CancellationTokenSource cancellationTokenSource in _cancellations.Values)
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            }

            bool isRunning = true;

            while (isRunning)
            {
                isRunning = false;
                foreach (var deploymentTargetWorker in _workers)
                {
                    if (deploymentTargetWorker.IsRunning)
                    {
                        isRunning = true;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            foreach (CancellationTokenSource cancellationTokenSource in _cancellations.Values)
            {
                try
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            }

            foreach (var worker in _workers)
            {
                worker.SafeDispose();
            }

            _workers.Clear();
            _cancellations.Clear();

            _isDisposed = true;
            _isDisposing = false;
        }
    }
}