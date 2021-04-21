using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Core.Settings;

namespace Milou.Deployer.Web.Marten
{
    public class InMemoryApplicationSettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = new();

        public Task<ApplicationSettings> GetApplicationSettings(CancellationToken cancellationToken) =>
            Task.FromResult(_settings);

        public Task Save(ApplicationSettings applicationSettings)
        {
            _settings = applicationSettings;

            return Task.CompletedTask;
        }
    }
}