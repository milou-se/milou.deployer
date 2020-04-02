using System;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentInstallConfiguration
    {
        public AgentInstallConfiguration(string name, string accessToken, Uri serverUri)
        {
            Name = name;
            AccessToken = accessToken;
            ServerUri = serverUri;
        }

        public string Name { get; }
        public string AccessToken { get; }
        public Uri ServerUri { get; }
    }
}