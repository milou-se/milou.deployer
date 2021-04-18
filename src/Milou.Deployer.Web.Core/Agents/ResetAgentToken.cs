 using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    [UsedImplicitly]
    public sealed record ResetAgentToken(AgentId AgentId) : ICommand<ResetAgentTokenResult>;
}