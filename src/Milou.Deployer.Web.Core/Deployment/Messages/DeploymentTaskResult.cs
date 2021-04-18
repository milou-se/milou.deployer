using System;
using Arbor.Processing;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentTaskResult
    {
        public DeploymentTaskResult(
            string deploymentTaskId,
            DeploymentTargetId deploymentTargetId,
            ExitCode exitCode,
            DateTime startedAtUtc,
            DateTime finishedAtUtc,
            string metadata)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            ExitCode = exitCode;
            StartedAtUtc = startedAtUtc;
            FinishedAtUtc = finishedAtUtc;
            Metadata = metadata;
        }

        [PublicAPI]
        public string DeploymentTaskId { get; }

        [PublicAPI]
        public DeploymentTargetId DeploymentTargetId { get; }

        public ExitCode ExitCode { get; }

        public DateTime StartedAtUtc { get; }

        public DateTime FinishedAtUtc { get; }

        public string Metadata { get; }

        public override string ToString() =>
            $"{nameof(DeploymentTaskId)}: {DeploymentTaskId}, {nameof(DeploymentTargetId)}: {DeploymentTargetId}, {nameof(ExitCode)}: {ExitCode}, {nameof(StartedAtUtc)}: {StartedAtUtc}, {nameof(FinishedAtUtc)}: {FinishedAtUtc}, {nameof(Metadata)}: {Metadata}";
    }
}