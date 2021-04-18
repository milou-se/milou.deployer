
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public sealed record CreateAgent(AgentId AgentId) : ICommand<CreateAgentResult>;
}