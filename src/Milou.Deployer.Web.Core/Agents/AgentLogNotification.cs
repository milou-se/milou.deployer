using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentLogNotification : IEvent
    {
        public AgentLogNotification(string deploymentTaskId, DeploymentTargetId deploymentTargetId, string message,
            LogEventLevel logEventLevel)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            Message = message;
            LogEventLevel = logEventLevel;
        }

        public DeploymentTargetId DeploymentTargetId { get; }

        public string Message { get; }

        public LogEventLevel LogEventLevel { get; }

        public string DeploymentTaskId { get; }
    }
}