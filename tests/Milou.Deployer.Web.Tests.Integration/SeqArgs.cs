using Arbor.Docker;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class SeqArgs
    {
        public SeqArgs(ContainerArgs containerArgs, PortPoolRental httpPort)
        {
            ContainerArgs = containerArgs;
            HttpPort = httpPort;
        }

        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental HttpPort { get; }
    }
}