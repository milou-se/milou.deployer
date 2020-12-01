using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Agents
{
    public class ResetAgentTokenResult : ICommandResult
    {
        public string Token { get; }

        public ResetAgentTokenResult(string token)
        {
            Token = token;
        }
    }
}