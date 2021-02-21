using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Milou.Deployer.Web.Core.Logging
{
    [UsedImplicitly]
    public class LogLevelModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder) => builder.AddSingleton<LogLevelState>();
    }
}