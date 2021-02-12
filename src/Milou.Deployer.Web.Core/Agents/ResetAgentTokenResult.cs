using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Agents
{
    public class ResetAgentTokenResult : ICommandResult
    {
        public string AccessToken { get; }

        public ResetAgentTokenResult(string accessToken) => AccessToken = accessToken;
    }
}