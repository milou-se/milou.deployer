using Arbor.Docker;

namespace Milou.Deployer.Web.Tests.Integration
{
    internal class Smtp4DevArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental SmtpPort { get; }
        public PortPoolRental HttpPort { get; }

        public Smtp4DevArgs(ContainerArgs containerArgs, PortPoolRental smtpPort, PortPoolRental httpPort)
        {
            ContainerArgs = containerArgs;
            SmtpPort = smtpPort;
            HttpPort = httpPort;
        }
    }
}