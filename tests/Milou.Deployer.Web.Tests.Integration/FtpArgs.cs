using Arbor.Docker;

namespace Milou.Deployer.Web.Tests.Integration
{
    internal class FtpArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental FtpDefault { get; }
        public PortPoolRental FtpSecondary { get; }

        public FtpArgs(ContainerArgs containerArgs, PortPoolRental ftpDefault, PortPoolRental ftpSecondary)
        {
            ContainerArgs = containerArgs;
            FtpDefault = ftpDefault;
            FtpSecondary = ftpSecondary;
        }
    }
}