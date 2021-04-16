using System;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.Marten.DeploymentTasks
{
    [MartenData]
    [PublicAPI]
    public record TaskLog
    {
        public string DeploymentTaskId { get; set; }

        public string DeploymentTargetId { get; set; }

        public string Id { get; set; }

        public DateTime FinishedAtUtc { get; set; }

        public string Status { get; set; }
    }
}