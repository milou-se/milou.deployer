using System.ComponentModel.DataAnnotations;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class CreateAgentInstallConfiguration : ICommand<AgentInstallConfiguration>
    {
        public CreateAgentInstallConfiguration(AgentId agentId) => AgentId = agentId;

        [Required]
        public AgentId AgentId { get; }
    }
}