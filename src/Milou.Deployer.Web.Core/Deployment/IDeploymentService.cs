﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Serilog;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Deployment
{
    public interface IDeploymentService
    {
        BlockingCollection<(string, WorkTaskStatus)> MessageQueue { get; }

        Task<DeploymentTaskResult> ExecuteDeploymentAsync(
            [NotNull] DeploymentTask deploymentTask,
            ILogger logger,
            CancellationToken cancellationToken);

        void Log(string message, LogEventLevel level = LogEventLevel.Information);
        void TaskDone(string deploymentTaskId);
        void TaskFailed(string deploymentTaskId);
    }
}