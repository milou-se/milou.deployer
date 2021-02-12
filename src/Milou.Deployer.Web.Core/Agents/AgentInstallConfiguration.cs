using System;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentInstallConfiguration : ICommandResult
    {
        public AgentInstallConfiguration(AgentId agentId, string accessToken, Uri serverUri)
        {
            AgentId = agentId;
            AccessToken = accessToken;
            ServerUri = serverUri;
        }

        public AgentId AgentId { get; }

        public string AccessToken { get; }

        public Uri ServerUri { get; }
    }
}