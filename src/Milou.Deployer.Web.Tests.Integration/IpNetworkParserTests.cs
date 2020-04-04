using System.Net;
using Milou.Deployer.Web.IisHost.Areas.Network;
using Xunit;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class IpNetworkParserTests
    {
        [Fact]
        public void BadIPv4NetworkLengthShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("127.0.0.1/a", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void DoubleSeparatorsShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("127.0.0.1//32", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void EmptyAddressShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void InvalidIpv4AddressShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("256.0.0.1/32", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void Ipv4NetworkOutOfRangeLengthShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("127.0.0.1/33", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void MissingSeparatorShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("127.0.0.1", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void NegativeIPv4NetworkLengthShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse("127.0.0.1/-1", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void NullAddressShouldNotBeParsed()
        {
            bool parsed = IpNetworkParser.TryParse(null, out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.False(parsed);
            Assert.Null(network);
        }

        [Fact]
        public void SingleIPv4AddressShouldBeParsed()
        {
            _ = IpNetworkParser.TryParse("127.0.0.1/32", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.NotNull(network);
            Assert.Equal(network.Prefix, IPAddress.Loopback);
            Assert.Equal(32, network.PrefixLength);
        }

        [Fact]
        public void SingleIPv6AddressShouldBeParsed()
        {
            _ = IpNetworkParser.TryParse("::1/32", out Microsoft.AspNetCore.HttpOverrides.IPNetwork network);

            Assert.NotNull(network);
            Assert.Equal(IPAddress.Parse("::1"), network.Prefix);
            Assert.Equal(32, network.PrefixLength);
        }
    }
}
