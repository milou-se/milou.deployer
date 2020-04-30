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
                new Dictionary<int, int> {[3125] = 80, [2526] = 25},
                new Dictionary<string, string> {["ServerOptions:TlsMode"] = "None"}
            );

            var postgresVariables = new Dictionary<string, string> {["POSTGRES_PASSWORD"] = "test"};

            string[] postgresArgs = {"-v", "deploydata:/var/lib/postgresql/data"};

            var postgres = new ContainerArgs(
                "postgres",
                "postgres-deploy",
                new Dictionary<int, int> {[5433] = 5432},
                postgresVariables,
                postgresArgs
            );

            string[] fptArgs = {/*"--net", "host"*/};
            var ftpVariables = new Dictionary<string, string>
            {
                ["FTP_USER"] = "testuser",
                ["FTP_PASS"] = "testpw",
            };

            var ftpPorts = new Dictionary<int, int>
            {
                [21] = 21,
                [20] = 20
            };

            for (int i = 21100; i <= 21110; i++)
            {
                ftpPorts.Add(i, i);
            }

            var ftp = new ContainerArgs(
                "fauria/vsftpd",
                "ftp",
                ftpPorts,
                ftpVariables,
                fptArgs
            );

            dockerArgs.Add(smtp4Dev);
            dockerArgs.Add(postgres);
            dockerArgs.Add(ftp);

            _dockerContext = await DockerContext.CreateContextAsync(dockerArgs, _logger);

            await _dockerContext.ContainerTask;

            _logger.Debug("Started containers {Containers}",
                string.Join(", ", _dockerContext.Containers.Select(container => container.Name)));
        }
    }
}
#endif