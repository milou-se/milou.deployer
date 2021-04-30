using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Logging;
using Arbor.Primitives;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.IisHost;
using Serilog;

namespace Milou.Deployer.Development
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            new LoggerConfiguration().WriteTo.Seq("http://localhost:5341");


            byte[] key = TokenHelper.GenerateKey();

            var agents = new List<string>() {"Agent1"};

            var devConfiguration = new DevConfiguration { ServerUrl = "http://localhost:34343", Key = new KeyData(key) };
            foreach (string agent in agents)
            {
                var agentId = new AgentId(agent);

                devConfiguration.Agents.Add(agentId, null);
            }

            var variables = EnvironmentVariables.GetEnvironmentVariables().Variables
                .ToDictionary(s => s.Key, s=>s.Value);

            variables.Add("urn:milou:deployer:web:milou-authentication:default:bearerTokenIssuerKey", devConfiguration.Key.KeyAsBase64);
            variables.Add("urn:milou:deployer:web:milou-authentication:default:enabled", "true");
            variables.Add("urn:milou:deployer:web:milou-authentication:default:bearerTokenEnabled", "true");
            variables.Add(LoggingConstants.SerilogSeqEnabledDefault, "true");
            variables.Add("urn:arbor:app:web:logging:serilog:default:seqUrl", "http://localhost:5341");
            variables.Add("urn:arbor:app:web:logging:serilog:default:consoleEnabled", "true");
            variables.Add(ConfigurationKeys.AllowPreReleaseEnvironmentVariable, "true");

            using var cancellationTokenSource = new CancellationTokenSource();
            var commonAssemblies = ApplicationAssemblies.FilteredAssemblies(new[] {"Arbor", "Milou"});
            IReadOnlyCollection<Assembly> serverAssemblies = commonAssemblies
                .Where(assembly => !assembly.GetName().Name!.Contains("Agent.Host")).ToImmutableArray();
            var serverTask = AppStarter.StartAsync(args, variables,
                cancellationTokenSource, serverAssemblies, new object[] {devConfiguration});

            return await serverTask;
        }
    }
}