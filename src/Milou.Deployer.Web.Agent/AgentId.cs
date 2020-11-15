using System;
using System.Diagnostics.CodeAnalysis;

namespace Milou.Deployer.Web.Agent
{
    public class AgentId : IEquatable<AgentId>
    {
        public bool Equals(AgentId? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((AgentId) obj);
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(AgentId? left, AgentId? right) => Equals(left, right);

        public static bool operator !=(AgentId? left, AgentId? right) => !Equals(left, right);

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