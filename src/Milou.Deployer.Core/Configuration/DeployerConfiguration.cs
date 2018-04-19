using System;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Configuration
{
    public class DeployerConfiguration
    {
        public WebDeployConfig WebDeploy { get; }

        public DeployerConfiguration([NotNull] WebDeployConfig webDeployConfig)
        {
            WebDeploy = webDeployConfig ?? throw new ArgumentNullException(nameof(webDeployConfig));
        }

        public string NuGetExePath { get; set; }

        public bool AllowPreReleaseEnabled { get; set; }

        public TimeSpan DefaultWaitTimeAfterAppOffline { get; set; } = TimeSpan.FromSeconds(3);
    }
}
