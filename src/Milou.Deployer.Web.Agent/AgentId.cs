﻿using System;
using System.Diagnostics.CodeAnalysis;
using Arbor.ModelBinding.Primitives;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    [StringValueType(StringComparison.OrdinalIgnoreCase)]
    public partial class AgentId
    {
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
    }
}