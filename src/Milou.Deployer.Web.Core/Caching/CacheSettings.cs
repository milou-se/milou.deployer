using Arbor.App.Extensions.Configuration;
using Arbor.KVConfiguration.Urns;

namespace Milou.Deployer.Web.Core.Caching
{
    [Urn(Urn)]
    [Optional]
    public class CacheSettings : IConfigurationValues
    {
        public string Host { get; }

        public const string Urn = "urn:arbor:app:caching:redis";

        public CacheSettings(string host)
        {
            Host = host;
        }
    }
}