#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
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

            await _dockerContext.DisposeAsync();

            _logger.Information("Disposed DockerContext");

            _isDisposed = true;
            _isDisposing = false;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var dockerArgs = new List<ContainerArgs>();

            var smtp4Dev = new ContainerArgs(
                "rnwood/smtp4dev:linux-amd64-v3",
                "smtp4devtest",
                new Dictionary<int, int> { [3125] = 80, [2526] = 25 }
            );

            var variables = new Dictionary<string, string> {["POSTGRES_PASSWORD"] = "test"};

            string[] postgresArgs = {"-v", "deploydata:/var/lib/postgresql/data"};

            var postgres = new ContainerArgs(
                "postgres",
                "postgres-deploy",
                new Dictionary<int, int> {[5433] = 5432},
                environmentVariables: variables,
                args: postgresArgs
            );

            dockerArgs.Add(smtp4Dev);
            dockerArgs.Add(postgres);

            _dockerContext = await DockerContext.CreateContextAsync(dockerArgs, _logger);

            await _dockerContext.ContainerTask;

            _logger.Debug("Started containers {Containers}", string.Join(", ", _dockerContext.Containers.Select(container => container.Name)));
        }
    }
}
#endif