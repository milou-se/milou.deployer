using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    [JsonConverter(typeof(AgentIdConverter))]
    public record AgentId
    {
        public AgentId(string value) => Value = value;

        public string Value { get; }

        public static bool TryParse(string? value, [NotNullWhen(true)] out AgentId? agentId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                agentId = null;
                return false;
            }

            agentId = new AgentId(value);
            return true;
        }

        public override string ToString() => Value;
    }
}