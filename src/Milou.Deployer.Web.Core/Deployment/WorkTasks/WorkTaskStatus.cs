using System;
using System.Linq;

namespace Milou.Deployer.Web.Core.Deployment.WorkTasks
{
    public sealed class WorkTaskStatus
    {
        public static readonly WorkTaskStatus Started = new(nameof(Started));

        public static readonly WorkTaskStatus Enqueued = new(nameof(Enqueued));

        public static readonly WorkTaskStatus Created = new(nameof(Created));

        public static readonly WorkTaskStatus Done = new(nameof(Done));

        public static readonly WorkTaskStatus Failed = new(nameof(Failed));

        public static readonly WorkTaskStatus Unknown = new(nameof(Unknown));

        private WorkTaskStatus(string status) => Status = status;

        public string Status { get; }

        public override string ToString() => Status;

        public static WorkTaskStatus ParseOrDefault(string itemStatus) =>
            EnumerableOf<WorkTaskStatus>.All.SingleOrDefault(status =>
                status.Status.Equals(itemStatus, StringComparison.OrdinalIgnoreCase)) ?? Unknown;
    }
}