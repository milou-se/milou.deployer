using MediatR;

namespace Milou.Deployer.Web.Core.Deployment
{
    public class WorkerCreated : INotification
    {
        public WorkerCreated(IDeploymentTargetWorker worker) => Worker = worker;
        public IDeploymentTargetWorker Worker { get; }
    }
}