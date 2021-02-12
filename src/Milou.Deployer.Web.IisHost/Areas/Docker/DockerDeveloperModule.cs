#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Configuration;
using Arbor.AspNetCore.Host;
using Arbor.Docker;
using JetBrains.Annotations;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Docker
{
    [RegistrationOrder(0)]
    [UsedImplicitly]
    public class DockerDeveloperModule : IPreStartModule, IAsyncDisposable
    {
        private readonly DeveloperConfiguration _developerConfiguration;
        private readonly ILogger _logger;
        private DockerContext? _dockerContext;
        private bool _isDisposed;
        private bool _isDisposing;

        public DockerDeveloperModule(ILogger logger, DeveloperConfiguration developerConfiguration)
        {
            _developerConfiguration = developerConfiguration;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposing || _isDisposed)
            {
                return;
            }

            _isDisposing = true;

            if (_dockerContext is {})
            {
                await _dockerContext.DisposeAsync();
            }

            _logger.Information("Disposed DockerContext");

            _isDisposed = true;
            _isDisposing = false;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!_developerConfiguration.DockerEnabled)
            {
                _logger.Debug("Developer Docker is disabled");
                return;
            }

            var dockerArgs = new List<ContainerArgs>();

            var smtp4Dev = CreateSmtp4Dev();
            dockerArgs.Add(smtp4Dev);

            var postgres = CreatePostgres();
            dockerArgs.Add(postgres);

            var ftp = CreateFtp();
            dockerArgs.Add(ftp);

            var redis = CreateRedis();
            dockerArgs.Add(redis);

            _dockerContext = await DockerContext.CreateContextAsync(dockerArgs, _logger);

            await _dockerContext.ContainerTask;

            _logger.Debug("Started containers {Containers}",
                string.Join(", ", _dockerContext.Containers.Select(container => container.Name)));
        }

        public int Order { get; } = 0;

        private static ContainerArgs CreateFtp()
        {
            var passivePorts = new PortRange(start: 23100, end: 23100);

            var ftpVariables = new Dictionary<string, string>
            {
                ["FTP_USER"] = "testuser",
                ["FTP_PASS"] = "testpw",
                ["PASV_MIN_PORT"] = passivePorts.Start.ToString(),
                ["PASV_MAX_PORT"] = passivePorts.End.ToString(),
            };

            var ftpPorts = new List<PortMapping>
            {
                PortMapping.MapSinglePort(hostPort: 20, containerPort: 20),
                PortMapping.MapSinglePort(hostPort: 21, containerPort: 21),
                new PortMapping(passivePorts, passivePorts)
            };

            var ftp = new ContainerArgs(
                "fauria/vsftpd",
                "ftp",
                ftpPorts,
                ftpVariables
            );

            return ftp;
        }

        private static ContainerArgs CreatePostgres()
        {
            var postgresVariables = new Dictionary<string, string>
            {
                ["POSTGRES_PASSWORD"] = "test"
            };

            string[] postgresArgs = {"-v", "deploydata:/var/lib/postgresql/data"};

            var postgres = new ContainerArgs(
                "postgres",
                "postgres-deploy",
                new List<PortMapping> {PortMapping.MapSinglePort(hostPort: 5433, containerPort: 5432)},
                postgresVariables,
                postgresArgs
            );

            return postgres;
        }

        private ContainerArgs CreateRedis()
        {
            var portMappings = new[] {PortMapping.MapSinglePort(hostPort: 26379, containerPort: 6379)};

            var redis = new ContainerArgs(
                "redis",
                "redistest",
                portMappings,
                args: new[] {"-v", "cachedata:/data"},
                entryPoint: new[] {"redis-server", "--appendonly yes"}
            );

            return redis;
        }

        private static ContainerArgs CreateSmtp4Dev()
        {
            var smtp4Dev = new ContainerArgs(
                "rnwood/smtp4dev:linux-amd64-v3",
                "smtp4devtest",
                new List<PortMapping>
                {
                    PortMapping.MapSinglePort(hostPort: 3125, containerPort: 80),
                    PortMapping.MapSinglePort(hostPort: 2526, containerPort: 25)
                },
                new Dictionary<string, string> {["ServerOptions:TlsMode"] = "None"}
            );

            return smtp4Dev;
        }
    }
}
#endif