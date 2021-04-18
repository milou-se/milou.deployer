using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.AspNetCore.Host;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Settings;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class ApplicationSettingsPreStartModule : IPreStartModule
    {
        private readonly IApplicationSettingsStore? _store;

        public ApplicationSettingsPreStartModule(IServiceProvider serviceProvider) => _store = serviceProvider.GetService<IApplicationSettingsStore>();

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (_store is {})
            {
                var applicationSettings = await _store.GetApplicationSettings(CancellationToken.None);

                applicationSettings.AutoDeploy.Enabled = true;
                applicationSettings.AutoDeploy.PollingEnabled = true;

                await _store.Save(applicationSettings);
            }
        }

        public int Order { get; }
    }
}