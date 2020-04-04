using System.Net;
using Milou.Deployer.Web.Core.Network;
using Xunit;

namespace Milou.Deployer.Web.Tests.Unit
{
    public class IpAddressExtensionsTests
    {
        [Fact]
        public void DifferentAddressesFamiliesShouldNotBeEqual()
        {
            bool equal = IPAddress.Parse("::1").EqualsAddress(IPAddress.Parse("127.0.0.1"));

            Assert.False(equal);
        }

        [Fact]
        public void DifferentIPv4AddressesShouldNotBeEqual()
        {
            bool equal = IPAddress.Parse("192.168.0.1").EqualsAddress(IPAddress.Parse("192.168.0.2"));

            Assert.False(equal);
        }

        [Fact]
        public void FirstNullAddressShouldNotBeEqual()
        {
            bool equal = IpAddressExtensions.EqualsAddress(null, IPAddress.Parse("127.0.0.1"));

            Assert.False(equal);
        }

        [Fact]
        public void LoopBackShouldBeEqual()
        {
            bool equal = IPAddress.Loopback.EqualsAddress(IPAddress.Loopback);

            Assert.True(equal);
        }

        [Fact]
        public void SecondNullAddressShouldNotBeEqual()
        {
            bool equal = IPAddress.Parse("127.0.0.1").EqualsAddress(null);

            Assert.False(equal);
        }
    }
}
