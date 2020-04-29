using Arbor.App.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Milou.Deployer.Web.Core.Extensions
{
    public class AppTimeModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder) => builder.AddSingleton<IAppTime, AppTime>(this);
    }
}