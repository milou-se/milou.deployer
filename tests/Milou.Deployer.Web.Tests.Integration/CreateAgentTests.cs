using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Marten;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Tests.Integration;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.IisHost.Areas.Agents;
using Milou.Deployer.Web.IisHost.Areas.Security;
using Milou.Deployer.Web.Marten;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class CreateAgentTests
    {
        [ConditionalFact]
        public async Task CreateAgentShouldReturnAgentWithIdAnToken()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IDocumentStore, TestStore>();

            serviceCollection.AddSingleton<IRequestHandler<CreateAgent, CreateAgentResult>, CreateAgentHandler>();
            serviceCollection
                .AddSingleton<IRequestHandler<CreateAgentInstallConfiguration, AgentInstallConfiguration>,
                    AgentConfigurationHelper>();

            serviceCollection.AddSingleton(new EnvironmentConfiguration {PublicHostname = "localhost"});

            using var hmac = new HMACSHA256();
            byte[] keyBytes = hmac.Key;
            string key = Convert.ToBase64String(keyBytes);

            serviceCollection.AddSingleton(new MilouAuthenticationConfiguration(true, true, key));

            var types = new[] {typeof(CreateAgentHandler)};
            serviceCollection.AddMediatR(types);

            var provider = serviceCollection.BuildServiceProvider();

            var mediator = provider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new CreateAgent(new AgentId("ExampleAgent")));
        }
    }
}