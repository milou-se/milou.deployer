using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using DotNext.Threading;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.IisHost.Areas.AutoDeploy;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Services
{
    public sealed class DeploymentTargetWorker : IDeploymentTargetWorker, IDisposable
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ICustomClock _clock;
        private readonly ILogger _logger;
        private readonly AsyncManualResetEvent _loggingCompleted = new AsyncManualResetEvent(false);
        private readonly IMediator _mediator;
        private readonly BlockingCollection<DeploymentTask> _queue = new BlockingCollection<DeploymentTask>();
        private readonly AsyncManualResetEvent _serviceAdded = new AsyncManualResetEvent(false);
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<string, IDeploymentService> _services =
            new ConcurrentDictionary<string, IDeploymentService>();

        private readonly BlockingCollection<DeploymentTask> _taskQueue = new BlockingCollection<DeploymentTask>();
        private readonly TimeoutHelper _timeoutHelper;
        private readonly WorkerConfiguration _workerConfiguration;
        private bool _isDisposed;

        public DeploymentTargetWorker(
            [NotNull] string targetId,
            [NotNull] ILogger logger,
            [NotNull] IMediator mediator,
            [NotNull] WorkerConfiguration workerConfiguration,
            TimeoutHelper timeoutHelper,
            ICustomClock clock,
            IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetId));
            }

            TargetId = targetId;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _workerConfiguration = workerConfiguration ?? throw new ArgumentNullException(nameof(workerConfiguration));
            _timeoutHelper = timeoutHelper;
            _clock = clock;
            _serviceProvider = serviceProvider;
        }

        public DeploymentTask CurrentTask { get; private set; }

        public bool IsRunning { get; private set; }

        public string TargetId { get; }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CheckDisposed();

            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            var messageTask = Task.Run(() => StartTaskMessageHandler(stoppingToken), stoppingToken);
            await StartProcessingAsync(stoppingToken);
            await messageTask;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            IsRunning = false;
            ClearQueue();
            _queue.SafeDispose();
            _taskQueue.SafeDispose();
            _loggingCompleted.SafeDispose();
            _serviceAdded.SafeDispose();
        }

        private async Task StartTaskMessageHandler(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && IsRunning)
            {
                DeploymentTask deploymentTask = _taskQueue.Take(stoppingToken);

                await _serviceAdded.WaitAsync(stoppingToken);

                if (!_services.TryGetValue(deploymentTask.DeploymentTaskId, out var deploymentService))
                {
                    throw new InvalidOperationException(
                        $"Could not get service associated to deployment task id {deploymentTask.DeploymentTaskId}");
                }

                while (!deploymentService.MessageQueue.IsCompleted)
                {
                    if (!deploymentService.MessageQueue.TryTake(
                        out (string Message, WorkTaskStatus Status) valueTuple,
                        TimeSpan.FromSeconds(_workerConfiguration.MessageTimeOutInSeconds)))
                    {
                        deploymentService.MessageQueue.CompleteAdding();
                    }

                    if (valueTuple.Message.HasValue())
                    {
                        await _mediator.Publish(
                            new DeploymentLogNotification(deploymentTask.DeploymentTargetId, valueTuple.Message),
                            stoppingToken);
                    }
                }

                _loggingCompleted.Set();

                _services.TryRemove(deploymentTask.DeploymentTaskId, out _);
            }
        }

        private async Task StartProcessingAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && IsRunning)
            {
                if (!IsRunning)
                {
                    return;
                }

                DeploymentTask? deploymentTask = default;
                IDeploymentService? service = default;

                try
                {
                    deploymentTask = _queue.Take(stoppingToken);

                    CurrentTask = deploymentTask;

                    service = _serviceProvider.GetRequiredService<IDeploymentService>();

                    if (!_services.TryAdd(deploymentTask.DeploymentTaskId, service))
                    {
                        throw new InvalidOperationException(
                            $"Could not add deployment service for deployment task id {deploymentTask.DeploymentTaskId}");
                    }

                    _serviceAdded.Set(false);

                    deploymentTask.Status = WorkTaskStatus.Started;
                    _taskQueue.Add(deploymentTask, stoppingToken);

                    _logger.Information("Deployment target worker has taken {DeploymentTask}", deploymentTask);

                    deploymentTask.Status = WorkTaskStatus.Started;

                    _logger.Information("Executing deployment task {DeploymentTask}", deploymentTask);

                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromHours(1));

                    using var combinedToken =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, stoppingToken);

                    DeploymentTaskResult result =
                        await service.ExecuteDeploymentAsync(deploymentTask, _logger, combinedToken.Token);

                    if (result.ExitCode.IsSuccess)
                    {
                        _logger.Information("Executed deployment task {DeploymentTask}", deploymentTask);

                        deploymentTask.Status = WorkTaskStatus.Done;
                        service.Log("Work task completed");
                    }
                    else
                    {
                        _logger.Error(
                            "Failed to deploy task {DeploymentTask}, result {Result}",
                            deploymentTask,
                            result.Metadata);

                        deploymentTask.Status = WorkTaskStatus.Failed;
                        service.Log("Work task failed");
                    }

                    await _loggingCompleted.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException operationCanceledException)
                {
                    _logger.Debug(operationCanceledException, "Taking next deployment task failed due to cancellation");
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    if (deploymentTask is {})
                    {
                        deploymentTask.Status = WorkTaskStatus.Failed;

                        if (ex is OperationCanceledException operationCanceledException)
                        {
                            _logger.Error(operationCanceledException, "Deployment Target Worker cancellation was triggered with ongoing task");
                        }
                        else
                        {
                            _logger.Error(ex, "Failed when executing deployment task {TaskId}",
                                deploymentTask.DeploymentTaskId);
                        }
                    }
                    else
                    {
                        if (ex is OperationCanceledException operationCanceledException)
                        {
                            _logger.Debug(operationCanceledException,
                                "Deployment Target Worker cancellation was triggered, no ongoing task");
                        }
                        else
                        {
                            _logger.Error(ex, "Failed when executing deployment");
                        }
                    }
                }
                finally
                {
                    CurrentTask = null!;
                    _serviceAdded.Reset();
                    _loggingCompleted.Reset();
                    service.SafeDispose();
                }
            }

            ClearQueue();
        }

        private void ClearQueue()
        {
            while (_queue.Count > 0)
            {
                try
                {
                    DeploymentTask deploymentTask = _queue.Take();

                    _logger.Debug("Ignored queued deployment task {DeploymentTask}", deploymentTask);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Error(ex, "Could not clear queue");
                }
            }
        }

        public void Enqueue([NotNull] DeploymentTask deploymentTask)
        {
            CheckDisposed();

            if (deploymentTask is null)
            {
                throw new ArgumentNullException(nameof(deploymentTask));
            }

            if (string.IsNullOrWhiteSpace(deploymentTask.DeploymentTargetId))
            {
                throw new ArgumentNullException(nameof(deploymentTask), "Target id is missing");
            }

            try
            {
                using CancellationTokenSource cts =
                    _timeoutHelper.CreateCancellationTokenSource(TimeSpan.FromSeconds(10));

                DeploymentTask[] tasksInQueue = _queue.ToArray();

                if (tasksInQueue.Length > 0
                    && tasksInQueue.Any(
                        queued =>
                            queued.PackageId.Equals(deploymentTask.PackageId, StringComparison.OrdinalIgnoreCase)
                            && queued.SemanticVersion.Equals(deploymentTask.SemanticVersion)))
                {
                    _logger.Warning(
                        "A deployment task with package id {PackageId} and version {Version} is already enqueued, skipping task, current queue length {Length}",
                        deploymentTask.PackageId,
                        deploymentTask.SemanticVersion.ToNormalizedString(),
                        tasksInQueue.Length);

                    return;
                }

                if (deploymentTask.StartedBy is {}
                    && deploymentTask.StartedBy.Equals(
                        nameof(AutoDeployBackgroundService),
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (CurrentTask is {}
                        && CurrentTask.SemanticVersion == deploymentTask.SemanticVersion
                        && CurrentTask.PackageId == deploymentTask.PackageId)
                    {
                        _logger.Warning(
                            "A deployment task {TaskId} is already executing as the new task trying to be added to queue, skipping new task {NewTaskId}",
                            CurrentTask?.DeploymentTaskId, deploymentTask.DeploymentTaskId);

                        return;
                    }

                    if (tasksInQueue.Length > 0
                        && tasksInQueue.Any(
                            queued => queued?.StartedBy?.Equals(
                                nameof(AutoDeployBackgroundService),
                                StringComparison.OrdinalIgnoreCase) == true))
                    {
                        _logger.Warning(
                            "A deployment task {TaskId} is already in queue as the new task trying to be added to queue, skipping new task {NewTaskId}",
                            CurrentTask?.DeploymentTaskId, deploymentTask.DeploymentTaskId);

                        return;
                    }
                }

                deploymentTask.Status = WorkTaskStatus.Enqueued;
                deploymentTask.EnqueuedAtUtc = _clock.UtcNow().UtcDateTime;
                _queue.Add(deploymentTask, cts.Token);

                _logger.Information(
                    "Enqueued deployment task {DeploymentTask}, current queue length {Length}",
                    deploymentTask,
                    tasksInQueue);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Failed to enqueue deployment task {DeploymentTask}", deploymentTask);
            }
        }

        internal Task StopAsync(CancellationToken stoppingToken)
        {
            CheckDisposed();

            IsRunning = false;

            ClearQueue();

            return Task.CompletedTask;
        }

        public ImmutableArray<TaskInfo> QueueInfo()
        {
            CheckDisposed();

            try
            {
                int queueCount = _queue.Count;

                if (queueCount > 0)
                {
                    var deploymentTasks = new DeploymentTask[queueCount];
                    _queue.CopyTo(deploymentTasks, 0);

                    var info = deploymentTasks
                        .Select(task => new TaskInfo(task.SemanticVersion, task.EnqueuedAtUtc))
                        .ToImmutableArray();

                    return info;
                }

                return ImmutableArray<TaskInfo>.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not get queue info for target {TargetId}", TargetId);

                return ImmutableArray<TaskInfo>.Empty;
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException($"{GetType().Name} {TargetId}");
            }
        }

        public void LogProgress(AgentLogNotification notification)
        {
            CheckDisposed();

            if (_services.TryGetValue(notification.DeploymentTaskId, out var service) &&
                !string.IsNullOrWhiteSpace(notification.Message))
            {
                service.Log(notification.Message);
            }
            else
            {
                _logger.Warning("Could not log agent log notification");
            }
        }

        public void NotifyDeploymentDone(AgentDeploymentDone notification)
        {
            CheckDisposed();

            if (_services.TryGetValue(notification.DeploymentTaskId, out var service))
            {
                service.TaskDone(notification.DeploymentTaskId);
            }
            else
            {
                _logger.Warning("Could not handle agent task done notification");
            }
        }

        public void NotifyDeploymentFailed(AgentDeploymentFailed notification)
        {
            CheckDisposed();

            if (_services.TryGetValue(notification.DeploymentTaskId, out var service))
            {
                service.TaskFailed(notification.DeploymentTaskId);
            }
            else
            {
                _logger.Warning("Could not handle agent failed notification");
            }
        }
    }
}