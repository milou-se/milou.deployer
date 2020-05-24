﻿using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class TargetCreated : IEvent
    {
        public TargetCreated(string targetId) => TargetId = targetId;

        public string TargetId { get; }
    }
}