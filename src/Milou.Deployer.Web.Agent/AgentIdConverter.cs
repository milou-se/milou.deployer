using System;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    public class AgentIdConverter : JsonConverter<AgentId>
    {
        public override bool CanWrite { get; } = false;

        public override void WriteJson(JsonWriter writer, AgentId value, JsonSerializer serializer) => throw new NotSupportedException();

        public override AgentId ReadJson(JsonReader reader,
            Type objectType,
            AgentId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) =>
            AgentId.TryParse(reader.Value as string, out var agentId)
                ? agentId
                : throw new FormatException(
                    $"Could not parse agent id from value '{reader.Value}'");
    }
}