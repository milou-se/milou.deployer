using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    [JsonConverter(typeof(AgentPoolNameConverter))]
    public record AgentPoolName(string Value)
    {
        public override string ToString() => Value ?? base.ToString();

        public static bool TryParse(string? value
            , [NotNullWhen(true)] out AgentPoolName? agentPoolName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                agentPoolName = default;
                return false;
            }

            agentPoolName = new AgentPoolName(value);
            return true;
        }
    };
}