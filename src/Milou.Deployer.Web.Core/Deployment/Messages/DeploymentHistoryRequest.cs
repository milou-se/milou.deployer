﻿using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentHistoryRequest : IQuery<DeploymentHistoryResponse>
    {
        public DeploymentHistoryRequest(string deploymentTargetId) => DeploymentTargetId = deploymentTargetId;

        public string DeploymentTargetId { get; }
    }
}