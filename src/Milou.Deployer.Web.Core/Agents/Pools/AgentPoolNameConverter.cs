using System;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AgentPoolNameConverter : JsonConverter<AgentPoolName>
    {
        public override bool CanWrite { get; } = false;

        public override void WriteJson(JsonWriter writer, AgentPoolName? value, JsonSerializer serializer) => throw new NotSupportedException();

        public override AgentPoolName ReadJson(JsonReader reader,
            Type objectType,
            AgentPoolName? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) =>
            AgentPoolName.TryParse(reader.Value as string, out var agentPoolName)
                ? agentPoolName
                : throw new FormatException(
                    $"Could not parse agent pool name from value '{reader.Value}'");
    }
}