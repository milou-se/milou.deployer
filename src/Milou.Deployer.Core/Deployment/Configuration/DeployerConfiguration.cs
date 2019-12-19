using System;

using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Configuration
{
    public class DeployerConfiguration
    {
        public DeployerConfiguration([NotNull] WebDeployConfig webDeployConfig) => WebDeploy = webDeployConfig ?? throw new ArgumentNullException(nameof(webDeployConfig));

        public WebDeployConfig WebDeploy { get; }

        public string NuGetExePath { get; set; }

        public string NuGetConfig { get; set; }

        public string NuGetSource { get; set; }

        public bool AllowPreReleaseEnabled { get; set; }

        public TimeSpan DefaultWaitTimeAfterAppOffline { get; set; } = TimeSpan.FromSeconds(3);

        public bool StopStartIisWebSiteEnabled { get; set; }
    }
}
