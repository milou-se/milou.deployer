#if DEBUG
using Arbor.KVConfiguration.Urns;

namespace Milou.Deployer.Web.IisHost.Areas.Docker
{
    [Urn(Urn)]
    [Optional]
    public class DeveloperConfiguration
    {
        public const string Urn = "urn:milou:deployer:web:development";

        public DeveloperConfiguration(bool dockerEnabled) => DockerEnabled = dockerEnabled;

        public bool DockerEnabled { get; }
    }
}
#endif