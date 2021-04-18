using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    [JsonConverter(typeof(AgentPoolNameConverter))]
    public record AgentPoolName
    {
        public AgentPoolName([JetBrains.Annotations.NotNull] string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public override string ToString() => Value;

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
    }
}