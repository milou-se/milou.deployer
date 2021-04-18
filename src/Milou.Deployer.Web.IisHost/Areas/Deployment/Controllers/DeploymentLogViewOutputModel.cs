using System.Collections.Generic;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Controllers
{
    public class DeploymentLogViewOutputModel
    {
        public DeploymentLogViewOutputModel(IReadOnlyCollection<LogItem> logItems) => LogItems = logItems;
        public IReadOnlyCollection<LogItem> LogItems { get; }
    }
}