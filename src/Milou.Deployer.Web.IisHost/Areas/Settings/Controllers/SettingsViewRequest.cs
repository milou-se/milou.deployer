using Arbor.App.Extensions.Messaging;
using MediatR;

using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Settings.Controllers
{
    public class SettingsViewRequest : IQuery<SettingsViewModel>
    {
    }
}