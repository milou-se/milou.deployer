using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Logging;
using Arbor.App.Extensions.Tasks;
using Arbor.AspNetCore.Host;
using Arbor.Primitives;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent.Host;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Milou.Deployer.Web.Core.Startup;

namespace Milou.Deployer.Development
{
    public class AgentManagerService : BackgroundService
    {
        private readonly DevConfiguration? _devConfiguration;
        private readonly StartupTaskContext? _startupTaskContext;

        public AgentManagerService(StartupTaskContext? startupTaskContext = null, DevConfiguration? devConfiguration = null)
        {
            _startupTaskContext = startupTaskContext;
            _devConfiguration = devConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            if (_startupTaskContext is null || _devConfiguration is null)
            {
                return;
            }

            var runners = new List<AgentRunner>();

            try
            {
                while (!_startupTaskContext.IsCompleted)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }

                var cancellationTokenSource = new CancellationTokenSource();

                foreach (var devConfigurationAgent in _devConfiguration.Agents)
                {
                    AgentRunner? agentRunner = default;
                    agentRunner = await StartAgent(devConfigurationAgent.Value, cancellationTokenSource);
                    runners.Add(agentRunner);
                }

                await cancellationTokenSource.Token;
            }
            catch (TaskCanceledException)
            {
                foreach (var agentRunner in runners)
                {
                    await agentRunner.StopAsync();
                }
            }
        }

        private async Task<AgentRunner> StartAgent(AgentConfiguration agentConfiguration,
            CancellationTokenSource cancellationTokenSource)
        {
            var instances = new object[] {agentConfiguration, agentConfiguration.AgentId()};
            var assemblies
                = ApplicationAssemblies.FilteredAssemblies().Where(assembly => !assembly.GetName().Name!.Contains("IisHost")).ToArray();

            var variables = EnvironmentVariables.GetEnvironmentVariables().Variables
                .ToDictionary(s => s.Key, s => s.Value);

            variables.Add(LoggingConstants.SerilogSeqEnabledDefault, "true");
            variables.Add("urn:arbor:app:web:logging:serilog:default:seqUrl", "http://localhost:5341");
            variables.Add("urn:arbor:app:web:logging:serilog:default:consoleEnabled", "true");


            var appTask = AppStarter<AgentStartup>.StartAsync(Array.Empty<string>(),
                variables, instances: instances, assemblies: assemblies
);
            var agentRunner = new AgentRunner(cancellationTokenSource, appTask);
            return agentRunner;
        }
    }
}