﻿using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class TargetDisabled : IEvent
    {
        public TargetDisabled(string targetId) => TargetId = targetId;

        public string TargetId { get; }
    }
}