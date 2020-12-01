using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.Logging;
using Arbor.AspNetCore.Host;
using Arbor.KVConfiguration.Urns;
using Arbor.Primitives;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Tests.Integration.TestData;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class AgentPreStartModule : IPreStartModule
    {
        private readonly ConfigurationInstanceHolder _holder;
        private readonly IServiceProvider _provider;
        private readonly SeqArgs? _seq;
        private readonly ServerEnvironmentTestConfiguration _serverConfiguration;
        private readonly TestConfiguration _testConfiguration;
        private CancellationTokenSource _agentCancellationTokenSource;

        public AgentPreStartModule(IServiceProvider provider,
            EnvironmentConfiguration environmentConfiguration)
        {
            if (environmentConfiguration.HttpEnabled)
            {
                _seq = provider.GetRequiredService<SeqArgs>();
                _provider = provider;
                _serverConfiguration = provider.GetRequiredService<ServerEnvironmentTestConfiguration>();
                _testConfiguration = provider.GetRequiredService<TestConfiguration>();
                _holder = provider.GetRequiredService<ConfigurationInstanceHolder>();
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (_seq is null)
            {
                return;
            }

            string seqUrl = $"http://localhost:{_seq.HttpPort}";

            var environmentVariables = new Dictionary<string, string>
            {
                [LoggingConstants.SerilogSeqEnabledDefault] = "true",
                ["urn:arbor:app:web:logging:serilog:default:seqUrl"] = seqUrl,
                ["urn:arbor:app:web:logging:serilog:default:consoleEnabled"] = "true"
            };

            var variables = new EnvironmentVariables(environmentVariables
            );
            var mediator = _provider.GetRequiredService<IMediator>();
            var createAgentResult = await mediator.Send(new CreateAgent(new AgentId("TestAgent")), cancellationToken);

            var instances =
                new List<object> {_testConfiguration, _serverConfiguration, variables}
                    .ToArray();

            _testConfiguration.AgentToken = createAgentResult.AccessToken;


            _agentCancellationTokenSource = new CancellationTokenSource();

            _agentCancellationTokenSource.Token.Register(() => Console.WriteLine("Agent is cancelled"));

            bool IsAgentAssembly(Assembly assembly)
            {
                bool isAgentAssembly = assembly.FullName is { } fullName &&
                                           (fullName.Contains("Arbor", StringComparison.Ordinal) ||
                                           fullName.Contains("Web.Agent", StringComparison.Ordinal) ||
                                            fullName.Contains("Web.Tests.Integration", StringComparison.Ordinal));
                return isAgentAssembly;
            }

            var assemblies = ApplicationAssemblies.FilteredAssemblies()
                .Where(IsAgentAssembly)
                .ToArray();

            var agentApp = await App<AgentStartup>.CreateAsync(_agentCancellationTokenSource, Array.Empty<string>(),
                variables.Variables,
                assemblies, instances);

            _holder.AddInstance(agentApp);

            var agentTask = agentApp.RunAsync(Array.Empty<string>());
            int result = await agentTask;

            if (result != 0)
            {
                throw new InvalidOperationException("Agent start failed");
            }
        }

        public int Order { get; } = 200;
    }
}