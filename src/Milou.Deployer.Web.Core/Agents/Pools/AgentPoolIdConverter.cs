using System;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AgentPoolIdConverter : JsonConverter<AgentPoolId>
    {
        public override bool CanWrite { get; } = false;

        public override void WriteJson(JsonWriter writer, AgentPoolId? value, JsonSerializer serializer) => throw new NotSupportedException();

        public override AgentPoolId ReadJson(JsonReader reader,
            Type objectType,
            AgentPoolId? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) =>
            AgentPoolId.TryParse(reader.Value as string, out var agentPoolId)
                ? agentPoolId
                : throw new FormatException(
                    $"Could not parse agent pool id from value '{reader.Value}'");
    }
}