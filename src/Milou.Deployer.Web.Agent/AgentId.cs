using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    [JsonConverter(typeof(AgentIdConverter))]
    public record AgentId
    {
        public AgentId(string value) => Value = value;

        public string Value { get; }

        public static AgentId Parse([JetBrains.Annotations.NotNull] string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }

            bool parsed = TryParse(value, out AgentId? agentId);

            if (!parsed)
            {
                throw new FormatException($"Invalid agent id {value}");
            }

            return agentId!;
        }

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