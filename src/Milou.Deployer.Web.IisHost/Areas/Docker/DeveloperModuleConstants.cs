#if DEBUG
using Arbor.KVConfiguration.Core.Metadata;

namespace Milou.Deployer.Web.IisHost.Areas.Docker
{
    public static class DeveloperModuleConstants
    {
        [Metadata(defaultValue: "false")]
        public const string DockerEnabledDefault = DeveloperConfiguration.Urn + ":default:docker-enabled";
    }
}
#endif