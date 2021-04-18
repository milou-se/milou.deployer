using Arbor.Docker;

namespace Milou.Deployer.Web.Tests.Integration
{
    internal class RedisArgs
    {
        public ContainerArgs ContainerArgs { get; }
        public PortPoolRental RedisPort { get; }

        public RedisArgs(ContainerArgs containerArgs, PortPoolRental redisPort)
        {
            ContainerArgs = containerArgs;
            RedisPort = redisPort;
        }
    }
}