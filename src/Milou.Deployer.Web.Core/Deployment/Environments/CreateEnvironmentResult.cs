using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.Core.Deployment.Environments
{
    public class CreateEnvironmentResult : ICommandResult
    {
        public CreateEnvironmentResult(string id, Result status)
        {
            Id = id;
            Status = status;
        }

        public string Id { get; }

        public Result Status { get; }
    }
}