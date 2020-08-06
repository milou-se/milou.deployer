using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Milou.Deployer.Waws
{
    internal class DeploymentBaseOptions
    {
        public DeploymentBaseOptions() => AuthenticationType = AuthenticationType.Basic;

        public TraceLevel TraceLevel { get; set; }

        public SkipDirectiveCollection SkipDirectives { get; } = new SkipDirectiveCollection();

        public string? ComputerName { get; set; }

        public string? Password { get; set; }

        public string? UserName { get; set; }

        public AuthenticationType? AuthenticationType { get; set; }

        public Action<object, DeploymentTraceEventArgs>? Trace { get; set; }

        public bool AllowUntrusted { get; set; }

        public string SiteName { get; set; }

        public static Task<DeploymentBaseOptions> Load(PublishSettings publishSettings) =>
            Task.FromResult(new DeploymentBaseOptions
            {
                Password = publishSettings.Password,
                ComputerName = publishSettings.ComputerName,
                AllowUntrusted = publishSettings.AllowUntrusted,
                AuthenticationType = AuthenticationType.Basic,
                UserName = publishSettings.Username
            });
    }
}