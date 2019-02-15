using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Milou.Deployer.Core.Deployment
{
    public class CustomEventArgs : EventArgs
    {
        public IDictionary<string, object> EventData { get; }

        public CustomEventArgs(IDictionary<string, object> eventData, TraceLevel eventLevel, string message)
        {
            this.EventData = eventData;
            this.EventLevel = eventLevel;
            Message = message;
        }

        public TraceLevel EventLevel { get; }
        public string Message { get; }
    }
}
