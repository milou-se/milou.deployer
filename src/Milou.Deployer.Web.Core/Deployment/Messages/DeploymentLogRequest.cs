using Arbor.App.Extensions.Messaging;
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