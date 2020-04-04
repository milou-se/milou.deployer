using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    internal class DeploymentBaseOptions
    {
        public DeploymentBaseOptions() => AuthenticationType = AuthenticationType.Basic;

        public TraceLevel TraceLevel { get; set; }

        public SkipDirectiveCollection SkipDirectives { get; } = new SkipDirectiveCollection();

        [CanBeNull]
        public string ComputerName { get; set; }

        [CanBeNull]
        public string Password { get; set; }

        [CanBeNull]
        public string UserName { get; set; }

        [CanBeNull]
        public AuthenticationType AuthenticationType { get; set; }

        [CanBeNull]
        public Action<object, DeploymentTraceEventArgs> Trace { get; set; }

        public bool AllowUntrusted { get; set; }

        public string SiteName { get; set; }

        public static async Task<DeploymentBaseOptions> Load(PublishSettings publishSettings) =>
            new DeploymentBaseOptions
            {
                Password = publishSettings.Password,
                ComputerName = publishSettings.ComputerName,
                AllowUntrusted = publishSettings.AllowUntrusted,
                AuthenticationType = AuthenticationType.Basic,
                UserName = publishSettings.Username
            };
    }
}