using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Milou.Deployer.Core.Deployment.WebDeploy
{
    public class CustomEventArgs : EventArgs
    {
        public CustomEventArgs(IDictionary<string, object> eventData, TraceLevel eventLevel, string message)
        {
            EventData = eventData;
            EventLevel = eventLevel;
            Message = message;
        }

        public IDictionary<string, object> EventData { get; }

        public TraceLevel EventLevel { get; }
        public string Message { get; }
    }
}