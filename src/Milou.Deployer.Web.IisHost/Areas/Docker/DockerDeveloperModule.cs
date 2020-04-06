#if DEBUG
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.AspNetCore.Host;
using Arbor.Docker;
using JetBrains.Annotations;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Docker
{
    [UsedImplicitly]
    public class DockerDeveloperModule : IPreStartModule, IAsyncDisposable
    {
        private readonly ILogger _logger;
        private DockerContext _dockerContext;
        private bool _isDisposed;

        private bool _isDisposing;

        public DockerDeveloperModule(ILogger logger) => _logger = logger;

        public async ValueTask DisposeAsync()
        {
            if (_isDisposing || _isDisposed)
            {
                return;
            }

            _isDisposing = true;

            _logger.Information("Disposed async");

            await _dockerContext.DisposeAsync();

            _isDisposed = true;
            _isDisposing = false;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ContainerArgs> dockerArgs = new List<ContainerArgs>();
            _dockerContext = await DockerContext.CreateContextAsync(dockerArgs, _logger);
        }
    }
}
#endif