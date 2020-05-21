using System.ComponentModel.DataAnnotations;
using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class CreateAgentInstallConfiguration : ICommand<AgentInstallConfiguration>
    {
        public CreateAgentInstallConfiguration(string agentName) => AgentName = agentName;

        [Required]
        public string AgentName { get; }
    }
}