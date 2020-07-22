using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.IO;
using Arbor.App.Extensions.Logging;
using Arbor.AspNetCore.Host;
using Arbor.Docker;
using Arbor.Primitives;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Configuration;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Controllers;
using Milou.Deployer.Web.IisHost.Areas.Security;
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
            "Server=localhost;Port={0};User Id=postgres;Password=test;Database=postgres;Pooling=false";
        private readonly IMessageSink _diagnosticMessageSink;
        private readonly DirectoryInfo _globalTempDir;
        private readonly string _oldTemp;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly Smtp4DevArgs _smtp4Dev;
        private readonly PostgresArgs _postgres;
        private readonly FtpArgs _ftp;
        private readonly RedisArgs _redis;
        private DockerContext _context;

        private EnvironmentVariables _environmentVariables;
        protected readonly IDictionary<string, string> _variables = new Dictionary<string, string>();
        private DirectoryInfo _appRootDirectory;
        private readonly SeqArgs _seq;
        private ILogger _testLogger;
        private readonly List<ContainerArgs> _dockerArgs;
        private Task<int> _agentTask;
        private CancellationTokenSource _agentCancellationTokenSource;
        private ImmutableArray<Assembly> _assemblies;

        protected WebFixtureBase(IMessageSink diagnosticMessageSink)
        {
            var id  = "-it";
            _assemblies = ApplicationAssemblies.FilteredAssemblies(new string[]{"Arbor", "Milou"});
            _globalTempDir =
                new DirectoryInfo(Path.Combine(Path.GetTempPath(), "mdst-" + Guid.NewGuid())).EnsureExists();

            _oldTemp = Path.GetTempPath();
            Environment.SetEnvironmentVariable("TEMP", _globalTempDir.FullName);
            _diagnosticMessageSink = diagnosticMessageSink;

            _dockerArgs = new List<ContainerArgs>();

            _smtp4Dev = CreateSmtp4Dev(id);
            _dockerArgs.Add(_smtp4Dev.ContainerArgs);

            _postgres = CreatePostgres(id);
            _dockerArgs.Add(_postgres.ContainerArgs);

            _ftp = CreateFtp(id);
            _dockerArgs.Add(_ftp.ContainerArgs);

            _redis = CreateRedis(id);
            _dockerArgs.Add(_redis.ContainerArgs);

            _seq = CreateSeq(id);
            _dockerArgs.Add(_seq.ContainerArgs);

            _variables.Add(LoggingConstants.SerilogSeqEnabledDefault, "true");
            string seqUrl = $"http://localhost:{_seq.HttpPort}";
            _variables.Add("urn:arbor:app:web:logging:serilog:default:seqUrl", seqUrl);
            Console.WriteLine($"Using seq url {seqUrl}");
        }

        private RedisArgs CreateRedis(string id)
        {
            var portRange = new PortPoolRange(10100, 100);
            var redisPort = TcpHelper.GetAvailablePort(portRange);
            var portMappings = new[] { PortMapping.MapSinglePort(redisPort.Port, 6379) };
            var redis = new ContainerArgs(
                "redis",
                "redistest"+ id,
                portMappings,
                args: Array.Empty<string>(),
                entryPoint: new[] { "redis-server" }
            );

            return new RedisArgs(redis, redisPort);
        }

        private static FtpArgs CreateFtp(string id)
        {
            var portRange = new PortPoolRange(10200, 100);
            var ftpDefault = TcpHelper.GetAvailablePort(portRange);
            var ftpSecondary = TcpHelper.GetAvailablePort(portRange);

            var passivePorts = new PortRange(start: 24100, end: 24100);
            var ftpVariables = new Dictionary<string, string>
            {
                ["FTP_USER"] = "testuser",
                ["FTP_PASS"] = "testpw",
                ["PASV_MIN_PORT"] = passivePorts.Start.ToString(),
                ["PASV_MAX_PORT"] = passivePorts.End.ToString()
            };

            var ftpPorts = new List<PortMapping>
            {
                PortMapping.MapSinglePort(ftpSecondary.Port, 20),
                PortMapping.MapSinglePort(ftpDefault.Port, 21),
                new PortMapping(passivePorts, passivePorts)
            };

            var ftp = new ContainerArgs(
                "fauria/vsftpd",
                "ftp" + id,
                ftpPorts,
                ftpVariables
            );

            return new FtpArgs(ftp, ftpDefault, ftpSecondary);
        }

        private static PostgresArgs CreatePostgres(string id)
        {
            var portRange = new PortPoolRange(10300, 100);
            var pgPort = TcpHelper.GetAvailablePort(portRange);
            var postgresVariables = new Dictionary<string, string> { ["POSTGRES_PASSWORD"] = "test" };

            var postgres = new ContainerArgs(
                "postgres",
                "postgres-deploy" + id,
                new List<PortMapping> { PortMapping.MapSinglePort(pgPort.Port, 5432) },
                postgresVariables
            );

            return new PostgresArgs(postgres, pgPort);
        }

        private static Smtp4DevArgs CreateSmtp4Dev(string id)
        {
           var portRange = new PortPoolRange(10000, 100);
           var smtpPort = TcpHelper.GetAvailablePort(portRange);
           var httpPort = TcpHelper.GetAvailablePort(portRange);

            var smtp4Dev = new ContainerArgs(
                "rnwood/smtp4dev:linux-amd64-v3",
                "smtp4devtest" + id,
                new List<PortMapping> { PortMapping.MapSinglePort(httpPort.Port, 80), PortMapping.MapSinglePort(smtpPort.Port, 25) },
                new Dictionary<string, string> { ["ServerOptions:TlsMode"] = "None" }
            );

            return new Smtp4DevArgs(smtp4Dev, smtpPort, httpPort);
        }
        private static SeqArgs CreateSeq(string id)
        {
           var portRange = new PortPoolRange(10400, 100);
           var httpPort = TcpHelper.GetAvailablePort(portRange);

            var args = new ContainerArgs(
                "datalust/seq:latest",
                "test-seq-"+id,
                new List<PortMapping> { PortMapping.MapSinglePort(httpPort.Port, 80) },
                new Dictionary<string, string> { ["ACCEPT_EULA"] = "Y" }
            );

            return new SeqArgs(args, httpPort);
        }

        public ServerEnvironmentTestConfiguration ServerEnvironmentTestSiteConfiguration { get; protected set; }

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

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public async Task InitializeAsync()
        {
            try
            {
                try
                {
                    _testLogger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.Debug()
                        .MinimumLevel.Verbose()
                        .CreateLogger();

                   _context = await DockerContext.CreateContextAsync(_dockerArgs, _testLogger);

                   await _context.ContainerTask;
                }
                finally
                {
                    _cancellationTokenSource =
                        new CancellationTokenSource(TimeSpan.FromSeconds(CancellationTimeoutInSeconds));
                }

                string connStr = string.Format(CultureInfo.InvariantCulture, ConnectionStringFormat,
                    _postgres.PgPort.Port);

                _variables.Add("urn:milou:deployer:web:marten:singleton:connection-string",
                    connStr);
                _variables.Add("urn:milou:deployer:web:marten:singleton:enabled", "true");

                _variables.Add(ApplicationConstants.DevelopmentMode.TrimStart('-'), "true");

                string rootDirectory = VcsTestPathHelper.GetRootDirectory();

                _appRootDirectory = new DirectoryInfo(Path.Combine(rootDirectory, "src", "Milou.Deployer.Web.IisHost"));

                var portPoolRange = new PortPoolRange(6200, 100);
                ServerEnvironmentTestSiteConfiguration = new ServerEnvironmentTestConfiguration(TcpHelper.GetAvailablePort(portPoolRange), _appRootDirectory);

                _variables.Add(DeployerAppConstants.SeedEnabled, "true");

                await BeforeInitialize(_cancellationTokenSource.Token);

                _environmentVariables = new EnvironmentVariables(_variables.ToImmutableDictionary());

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

                try
                {
                    await StartAsync(args);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    if (App?.Host?.Services?.GetService<IHostApplicationLifetime>() is {} hostApplicationLifetime
                        && !hostApplicationLifetime.ApplicationStopped.IsCancellationRequested)
                    {
                        hostApplicationLifetime.StopApplication();
                    }

                    _diagnosticMessageSink.OnMessage(new DiagnosticMessage(ex.ToString()));

                    Exception = ex;

                    _cancellationTokenSource.Cancel();

                    return;
                }

                IHostApplicationLifetime? appLifeTime = null;

                if (App?.Host?.Services?.GetService<IHostApplicationLifetime>() is { } lifeTime
                    && !lifeTime.ApplicationStopped.IsCancellationRequested)
                {
                    appLifeTime = lifeTime;
                }

                if (appLifeTime is null)
                {
                    throw new InvalidOperationException("Lifetime is null");
                }

                while (!appLifeTime.ApplicationStarted.IsCancellationRequested && !CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken);
                }

                if (CancellationToken.IsCancellationRequested)
                {
                    throw new DeployerAppException("The cancellation token is already cancelled, skipping run");
                }

                await StartAgents();

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


        private async Task StartAgents()
        {
            var mediator=  App.Host.Services.GetRequiredService<IMediator>();
            var createAgentResult = await mediator.Send(new CreateAgent(new AgentId("TestAgent")));

            var serilogConfiguration = new SerilogConfiguration(seqUrl: "http://localhost:"+ _seq.HttpPort, rollingLogFilePath: null, seqEnabled: true);
            var instances = new List<object>() {TestConfiguration, ServerEnvironmentTestSiteConfiguration, serilogConfiguration }.ToArray();

            TestConfiguration.AgentToken = createAgentResult.AccessToken;

            var variables = new EnvironmentVariables(new Dictionary<string, string>()).Variables;

            _agentCancellationTokenSource = new CancellationTokenSource();

            _agentCancellationTokenSource.Token.Register(() => Console.WriteLine("Agent is cancelled"));

            bool IsAgentAssembly(Assembly assembly)
            {
                return assembly.FullName is {} fullName &&
                       (fullName.Contains("Web.Agent", StringComparison.Ordinal)||
                        fullName.Contains("Tests"));
            }

            var assemblies = ApplicationAssemblies.FilteredAssemblies()
                .Where(IsAgentAssembly).ToArray();

            _agentTask = Task.Run(() => AppStarter<AgentStartup>.StartAsync(Array.Empty<string>(), variables, assemblies: assemblies, instances: instances));
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

            try
            {
                //_agentCancellationTokenSource.Cancel();
                //await _agentTask;
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                _agentCancellationTokenSource.SafeDispose();
            }

            await _context.DisposeAsync();
            _testLogger.SafeDispose();
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
            string[] args = {$"{ConfigurationConstants.ContentBasePath}={_appRootDirectory}"};

            _cancellationTokenSource.Token.Register(() => Console.WriteLine("App cancellation token triggered in test"));

            object[] instances =
            {
                TestConfiguration,
                ServerEnvironmentTestSiteConfiguration,
                new CacheSettings(),
                _environmentVariables,
                new ApplicationPartManager(),
                new MilouAuthenticationConfiguration(true, true, "+LZwHMY/0pifza3BAmrxwzt8F+G+KdMmBfe6nUhqqI9cIZXOLHaYRa0TRldq5ocrBkRELPSCqpEkEKtQvM9FSw==")
            };

            var assemblies = _assemblies
                .Where(a => a.FullName is string fullName && !fullName.Contains("Agent.Host")).ToImmutableArray();

            App = await App<ApplicationPipeline>.CreateAsync(_cancellationTokenSource, args,
                _environmentVariables.Variables, assemblies, instances);

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
}