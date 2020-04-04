using System.Linq;
using System.Net;

namespace Milou.Deployer.Web.Core.Network
{
    public static class IpAddressExtensions
    {
        public static bool EqualsAddress(this IPAddress? address, IPAddress? otherAddress)
        {
            if (address is null || otherAddress is null)
            {
                return false;
            }

            bool sameFamily = address.AddressFamily == otherAddress.AddressFamily;

            if (!sameFamily)
            {
                return false;
            }

            return address.GetAddressBytes().SequenceEqual(otherAddress.GetAddressBytes());
        }
    }
}
