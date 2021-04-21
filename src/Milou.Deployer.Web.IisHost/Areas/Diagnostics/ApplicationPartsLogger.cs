using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Diagnostics
{
    public class ApplicationPartsLogger : IHostedService
    {
        private readonly ILogger _logger;
        private readonly ApplicationPartManager? _partManager;

        public ApplicationPartsLogger(ILogger logger, ApplicationPartManager? partManager = null)
        {
            _logger = logger;
            _partManager = partManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_partManager is null)
            {
                _logger.Debug("Application parts logging is disabled due to missing {Manager}", nameof(ApplicationPartManager));
                return Task.CompletedTask;
            }

            var applicationParts = _partManager.ApplicationParts.Select(x => x.Name);

            var controllerFeature = new ControllerFeature();
            _partManager.PopulateFeature(controllerFeature);

            var controllers = controllerFeature.Controllers.Select(x => x.Name);

            _logger.Debug(
                "Found the following application parts: '{ApplicationParts}' with the following controllers: '{Controllers}'",
                string.Join(", ", applicationParts), string.Join(", ", controllers));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}