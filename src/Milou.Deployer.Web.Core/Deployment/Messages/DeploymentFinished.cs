﻿using System;
using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core.Deployment.Targets;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentFinished : IEvent
    {
        public DeploymentFinished(
            [NotNull] DeploymentTask deploymentTask,
            LogItem[] logLines,
            DateTime finishedAtUtc)
        {
            LogLines = logLines.ToImmutableArray();
            FinishedAtUtc = finishedAtUtc;
            DeploymentTask = deploymentTask ?? throw new ArgumentNullException(nameof(deploymentTask));
        }

        public ImmutableArray<LogItem> LogLines { get; }

        public DeploymentTask DeploymentTask { get; }

        public DateTime FinishedAtUtc { get; }
    }
}