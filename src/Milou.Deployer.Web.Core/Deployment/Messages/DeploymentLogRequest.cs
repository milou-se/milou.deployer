using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentLogRequest : IQuery<DeploymentLogResponse>
    {
        public DeploymentLogRequest(string deploymentTaskId, LogEventLevel level)
        {
            DeploymentTaskId = deploymentTaskId;
            Level = level;
        }

        public string DeploymentTaskId { get; }

        public LogEventLevel Level { get; }
    }
}