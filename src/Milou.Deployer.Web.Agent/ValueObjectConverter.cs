using System;
using Arbor.App.Extensions.ExtensionMethods;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    public class ValueObjectConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            if (value.GetType().GetProperty("Value") is { } property)
            {
                writer.WriteValue(property.GetValue(value));
                return;
            }

            writer.WriteValue(value);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            if (existingValue is null)
            {
                return null;
            }

            return Activator.CreateInstance(objectType, new[] {existingValue});
        }

        public override bool CanConvert(Type objectType) => !objectType.IsAbstract && objectType.BaseType.Closes(typeof(ValueObject<,>));
    }
}