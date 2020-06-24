using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.IO;
using Arbor.AspNetCore.Host;
using Arbor.Docker;
using Arbor.Primitives;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Controllers;
using Milou.Deployer.Web.IisHost.AspNetCore.Startup;
using Milou.Deployer.Web.Marten;
using Milou.Deployer.Web.Marten.Abstractions;
using Milou.Deployer.Web.Tests.Integration.TestData;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Milou.Deployer.Web.Tests.Integration
{
    public abstract class WebFixtureBase : IDisposable, IAsyncLifetime
    {
        private const int CancellationTimeoutInSeconds = 180;

        private const string ConnectionStringFormat =
            "Server=localhost;Port={0};User Id={1};Password=test;Database=postgres;Pooling=false";

        private static readonly string PostgresqlUser = "postgres";
        private readonly IMessageSink _diagnosticMessageSink;
        private readonly DirectoryInfo _globalTempDir;
        private readonly string _oldTemp;

        private CancellationTokenSource _cancellationTokenSource;
        private Smtp4DevArgs _smtp4Dev;
        private PostgresArgs _postgres;
        private FtpArgs _ftp;
        private RedisArgs _redis;
        private DockerContext _context;

        protected WebFixtureBase(IMessageSink diagnosticMessageSink)
        {
            _globalTempDir =
                new DirectoryInfo(Path.Combine(Path.GetTempPath(), "mdst-" + Guid.NewGuid())).EnsureExists();

            _oldTemp = Path.GetTempPath();
            Environment.SetEnvironmentVariable("TEMP", _globalTempDir.FullName);
            _diagnosticMessageSink = diagnosticMessageSink;

            var dockerArgs = new List<ContainerArgs>();

            _smtp4Dev = CreateSmtp4Dev();
            dockerArgs.Add(_smtp4Dev.ContainerArgs);

            _postgres = CreatePostgres();
            dockerArgs.Add(_postgres.ContainerArgs);

            _ftp = CreateFtp();
            dockerArgs.Add(_ftp.ContainerArgs);

            _redis = CreateRedis();
            dockerArgs.Add(_redis.ContainerArgs);
        }

        private RedisArgs CreateRedis()
        {
            var portRange = new PortPoolRange(10100, 100);
            var redisPort = TcpHelper.GetAvailablePort(portRange);
            var portMappings = new[] { PortMapping.MapSinglePort(redisPort.Port, 6379) };
            var redis = new ContainerArgs(
                "redis",
                "redistest",
                portMappings,
                args: Array.Empty<string>(),
                entryPoint: new[] { "redis-server" }
            );

            return new RedisArgs(redis, redisPort);
        }

        private static FtpArgs CreateFtp()
        {
            var portRange = new PortPoolRange(10200, 100);
            var ftpDefault = TcpHelper.GetAvailablePort(portRange);
            var ftpSecondary = TcpHelper.GetAvailablePort(portRange);
            var ftpVariables = new Dictionary<string, string> { ["FTP_USER"] = "testuser", ["FTP_PASS"] = "testpw" };

            var passivePorts = new PortRange(21100, 21110);

            var ftpPorts = new List<PortMapping>
            {
                PortMapping.MapSinglePort(ftpSecondary.Port, 20),
                PortMapping.MapSinglePort(ftpDefault.Port, 21),
                new PortMapping(passivePorts, passivePorts)
            };

            var ftp = new ContainerArgs(
                "fauria/vsftpd",
                "ftp",
                ftpPorts,
                ftpVariables
            );

            return new FtpArgs(ftp, ftpDefault, ftpSecondary);
        }

        private static PostgresArgs CreatePostgres()
        {
            var portRange = new PortPoolRange(10300, 100);
            var pgPort = TcpHelper.GetAvailablePort(portRange);
            var postgresVariables = new Dictionary<string, string> { ["POSTGRES_PASSWORD"] = "test" };

            var postgres = new ContainerArgs(
                "postgres",
                "postgres-deploy",
                new List<PortMapping> { PortMapping.MapSinglePort(pgPort.Port, 5432) },
                postgresVariables
            );

            return new PostgresArgs(postgres, pgPort);
        }

        private static Smtp4DevArgs CreateSmtp4Dev()
        {
           var portRange = new PortPoolRange(10000, 100);
           var smtpPort = TcpHelper.GetAvailablePort(portRange);
           var httpPort = TcpHelper.GetAvailablePort(portRange);

            var smtp4Dev = new ContainerArgs(
                "rnwood/smtp4dev:linux-amd64-v3",
                "smtp4devtest",
                new List<PortMapping> { PortMapping.MapSinglePort(httpPort.Port, 80), PortMapping.MapSinglePort(smtpPort.Port, 25) },
                new Dictionary<string, string> { ["ServerOptions:TlsMode"] = "None" }
            );

            return new Smtp4DevArgs(smtp4Dev, smtpPort, httpPort);
        }

        public TestHttpPort TestSiteHttpPort { get; protected set; }

        [PublicAPI]
        public List<DirectoryInfo> DirectoriesToClean { get; } = new List<DirectoryInfo>();

        [PublicAPI]
        public TestConfiguration TestConfiguration { get; protected set; }

        [PublicAPI]
        public List<FileInfo> FilesToClean { get; } = new List<FileInfo>();

        public Exception Exception { get; private set; }

        public App<ApplicationPipeline> App { get; private set; }

        [PublicAPI]
        public int? HttpPort => GetHttpPort();

        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public async Task InitializeAsync()
        {
            try
            {
                try
                {
                    var testLogger = Logger.None;
                    var containerArgs = new List<ContainerArgs>
                    {
                        _postgres.ContainerArgs,
                        _smtp4Dev.ContainerArgs,
                        _ftp.ContainerArgs,
                        _redis.ContainerArgs,
                    };

                   _context = await DockerContext.CreateContextAsync(containerArgs, testLogger);
                }
                finally
                {
                    _cancellationTokenSource =
                        new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTimeoutInSeconds));
                }

                string connStr = string.Format(CultureInfo.InvariantCulture, ConnectionStringFormat, _postgres.PgPort.Port,
                    PostgresqlUser);

                Environment.SetEnvironmentVariable("urn:milou:deployer:web:marten:singleton:connection-string",
                    connStr);
                Environment.SetEnvironmentVariable("urn:milou:deployer:web:marten:singleton:enabled", "true");

                await BeforeInitialize(_cancellationTokenSource.Token);
                IReadOnlyCollection<string> args = await RunSetupAsync();

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new DeployerAppException(
                        "The cancellation token is already cancelled, skipping before start");
                }

                try
                {
                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage("Running before start"));

                    await BeforeStartAsync(args);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage(ex.ToString()));
                    _cancellationTokenSource.Cancel();
                    throw new DeployerAppException("Before start exception", ex);
                }

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new DeployerAppException("The cancellation token is already cancelled, skipping start");
                }

                await StartAsync(args);

                await Task.Delay(TimeSpan.FromSeconds(1));

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new DeployerAppException("The cancellation token is already cancelled, skipping run");
                }

                await RunAsync();

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new DeployerAppException("The cancellation token is already cancelled, skipping after run");
                }

                await AfterRunAsync();
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                Exception = ex;
                OnException(ex);
            }
        }

        public virtual async Task DisposeAsync()
        {
            App?.Logger?.Information("Stopping app from {Type}", GetType().FullName);
            _cancellationTokenSource?.Dispose();
            App?.Dispose();

            FileInfo[] files = FilesToClean.ToArray();

            foreach (FileInfo fileInfo in files)
            {
                try
                {
                    fileInfo.Refresh();
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }

                FilesToClean.Remove(fileInfo);
            }

            DirectoryInfo[] directoryInfos = DirectoriesToClean.ToArray();

            foreach (DirectoryInfo directoryInfo in directoryInfos.OrderByDescending(x => x.FullName.Length))
            {
                await DeleteDirectoryAsync(directoryInfo);
                DirectoriesToClean.Remove(directoryInfo);
            }

            Environment.SetEnvironmentVariable("TEMP", _oldTemp);

            await DeleteDirectoryAsync(_globalTempDir);

            await _context.DisposeAsync();
        }

        public virtual void Dispose() => GC.SuppressFinalize(this);

        private int? GetHttpPort()
        {
            EnvironmentConfiguration? environmentConfiguration =
                App.Host!.Services.GetService<EnvironmentConfiguration>();

            return environmentConfiguration?.HttpPort;
        }

        private async Task DeleteDirectoryAsync(DirectoryInfo directoryInfo, int attempt = 0)
        {
            if (attempt == 5)
            {
                return;
            }

            try
            {
                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }

                directoryInfo.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"could not delete directory {directoryInfo.FullName}", ex);
                // ignore

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            if (directoryInfo.Exists)
            {
                await DeleteDirectoryAsync(directoryInfo, attempt + 1);
            }
        }

        private async Task StartAsync(IReadOnlyCollection<string> args)
        {
            App.Logger.Information("Starting app");

            await App.RunAsync(args.ToArray());

            App.Logger.Information("Started app, waiting for web host shutdown");
        }

        private async Task<IReadOnlyCollection<string>> RunSetupAsync()
        {
            string rootDirectory = VcsTestPathHelper.GetRootDirectory();

            string appRootDirectory = Path.Combine(rootDirectory, "src", "Milou.Deployer.Web.IisHost");

            string[] args = {$"{ConfigurationConstants.ContentBasePath}={appRootDirectory}"};

            _cancellationTokenSource.Token.Register(() => Console.WriteLine("App cancellation token triggered"));

            App = await App<ApplicationPipeline>.CreateAsync(_cancellationTokenSource, args,
                EnvironmentVariables.GetEnvironmentVariables().Variables, TestConfiguration, TestSiteHttpPort);

            App.Logger.Information("Restart time is set to {RestartIntervalInSeconds} seconds",
                CancellationTimeoutInSeconds);

            return args;
        }

        protected virtual void OnException(Exception exception)
        {
        }

        protected virtual Task AfterRunAsync() => Task.CompletedTask;

        protected virtual Task BeforeStartAsync(IReadOnlyCollection<string> args) => Task.CompletedTask;

        protected virtual Task BeforeInitialize(CancellationToken cancellationToken) => Task.CompletedTask;

        protected abstract Task RunAsync();
    }

    internal class Smtp4DevArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental SmtpPort { get; }
        public PortPoolRental HttpPort { get; }

        public Smtp4DevArgs(ContainerArgs containerArgs, PortPoolRental smtpPort, PortPoolRental httpPort)
        {
            ContainerArgs = containerArgs;
            SmtpPort = smtpPort;
            HttpPort = httpPort;
        }
    }

    internal class RedisArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental RedisPort { get; }

        public RedisArgs(ContainerArgs containerArgs, PortPoolRental redisPort)
        {
            ContainerArgs = containerArgs;
            RedisPort = redisPort;
        }
    }

    internal class PostgresArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental PgPort { get; }

        public PostgresArgs(ContainerArgs containerArgs, PortPoolRental pgPort)
        {
            ContainerArgs = containerArgs;
            PgPort = pgPort;
        }
    }

    internal class FtpArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental FtpDefault { get; }
        public PortPoolRental FtpSecondary { get; }

        public FtpArgs(ContainerArgs containerArgs, PortPoolRental ftpDefault, PortPoolRental ftpSecondary)
        {
            ContainerArgs = containerArgs;
            FtpDefault = ftpDefault;
            FtpSecondary = ftpSecondary;
        }
    }
}