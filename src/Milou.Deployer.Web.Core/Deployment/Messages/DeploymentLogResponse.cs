using System.Collections.Generic;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentLogResponse : IQueryResult
    {
        public DeploymentLogResponse(IReadOnlyCollection<LogItem> logItems) => LogItems = logItems;

        public IReadOnlyCollection<LogItem> LogItems { get; }
    }
}