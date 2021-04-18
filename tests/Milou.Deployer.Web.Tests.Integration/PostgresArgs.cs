using Arbor.Docker;

namespace Milou.Deployer.Web.Tests.Integration
{
    internal class PostgresArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental PgPort { get; }

        public PostgresArgs(ContainerArgs containerArgs, PortPoolRental pgPort)
        {
            ContainerArgs = containerArgs;
            PgPort = pgPort;
        }
    }
}