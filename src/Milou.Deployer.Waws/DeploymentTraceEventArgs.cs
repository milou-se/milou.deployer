using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    internal class DeploymentTraceEventArgs
    {
        public IDictionary<string, object> EventData { get; } = new ConcurrentDictionary<string, object>();

        public TraceLevel EventLevel { get; set; }

        [CanBeNull]
        public string Message { get; set; }
    }
}