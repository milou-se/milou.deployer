using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.KVConfiguration.Urns;
using DotNext.Threading;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.Core.Startup;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Services;
using Milou.Deployer.Web.Tests.Integration.TestData;
using Serilog;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class AutoDeployStartupTask : BackgroundService, IStartupTask, INotificationHandler<DeploymentFinished>
    {
        private readonly EnvironmentConfiguration _environmentConfiguration;
        private readonly ILogger _logger;
        private readonly IDeploymentTargetReadService _readService;
        private readonly TestConfiguration? _testConfiguration;
        private readonly ServerEnvironmentTestConfiguration _serverEnvironmentTestSiteConfiguration;
        private IWebHost _webHost;
        private readonly AsyncManualResetEvent _handle = new AsyncManualResetEvent(false);
        private readonly DeploymentWorkerService _worker;

        public AutoDeployStartupTask(
            IServiceProvider serviceProvider,
            EnvironmentConfiguration environmentConfiguration)
        {
            _environmentConfiguration = environmentConfiguration;
            if (environmentConfiguration.HttpEnabled)
            {
                _worker = serviceProvider.GetRequiredService<DeploymentWorkerService>();

                _testConfiguration = serviceProvider.GetRequiredService<TestConfiguration>();
                var testHttpPorts = serviceProvider.GetRequiredService<ConfigurationInstanceHolder>()
                    .GetInstances<ServerEnvironmentTestConfiguration>().Values;
                _serverEnvironmentTestSiteConfiguration = testHttpPorts.FirstOrDefault() ??
                                                          throw new InvalidOperationException("Missing test http port");
                _logger = serviceProvider.GetRequiredService<ILogger>();
                _readService = serviceProvider.GetRequiredService<IDeploymentTargetReadService>();
            }
        }

        public bool IsCompleted { get; private set; }

        protected override async Task ExecuteAsync(CancellationToken startupCancellationToken)
        {
            await Task.Yield();

            if (_testConfiguration is null || !_environmentConfiguration.HttpEnabled)
            {
                IsCompleted = true;
                return;
            }

            ImmutableArray<DeploymentTarget> targets = ImmutableArray<DeploymentTarget>.Empty;

            while (targets.IsDefaultOrEmpty && !startupCancellationToken.IsCancellationRequested)
            {
                targets = await _readService.GetDeploymentTargetsAsync(stoppingToken: startupCancellationToken);

                if (targets.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), startupCancellationToken);
                }
                else
                {
                    _logger.Debug("The test target has now yet been created");
                    break;
                }
            }

            const string packageVersion = "MilouDeployerWebTest 1.2.4";

            var deploymentTaskId = Guid.NewGuid();
            const string deploymentTargetId = TestDataCreator.Testtarget;
            var deploymentTask = new DeploymentTask(packageVersion, deploymentTargetId, deploymentTaskId,
                nameof(AutoDeployStartupTask));

            await _worker.Enqueue(deploymentTask);

            await _handle.WaitAsync(startupCancellationToken);

            int testSitePort = _serverEnvironmentTestSiteConfiguration.Port.Port + 1;

            try
            {
                _webHost = WebHost.CreateDefaultBuilder()
                    .ConfigureServices(services => services.AddSingleton(_testConfiguration))
                    .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Loopback,
                            testSitePort);
                    })
                    .UseContentRoot(_testConfiguration.SiteAppRoot.FullName)
                    .UseStartup<TestStartup>().Build();

                await _webHost.StartAsync(startupCancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Could not start test site on port {Port}", testSitePort);

                throw new DeployerAppException($"Could not start test site on port {testSitePort}", ex);
            }

            startupCancellationToken.Register(() => _webHost.StopAsync(startupCancellationToken));
            HttpResponseMessage? response = default;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var uri = new Uri($"http://localhost:{testSitePort}/applicationmetadata.json");
                    response = await httpClient.GetAsync(uri, startupCancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Information("Successfully made get request to test site {Status}", response.StatusCode);
                    }
                    else
                    {
                        _logger.Information("Failed to make get request to test site {Status}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Could not get successful http get response in integration test, {Status}",
                        response?.StatusCode);
                }
            }

            IsCompleted = true;
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();
            _webHost.SafeDispose();
            _serverEnvironmentTestSiteConfiguration.SafeDispose();
        }

        public Task Handle(DeploymentFinished notification, CancellationToken cancellationToken)
        {
            _handle.Set();

            return Task.CompletedTask;
        }
    }
}