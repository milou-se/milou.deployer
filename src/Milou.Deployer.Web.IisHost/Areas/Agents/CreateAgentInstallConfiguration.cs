using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class CreateAgentInstallConfiguration : IRequest<AgentInstallConfiguration>
    {
        public CreateAgentInstallConfiguration(string agentName) => AgentName = agentName;

        [Required]
        public string AgentName { get; }
    }
}