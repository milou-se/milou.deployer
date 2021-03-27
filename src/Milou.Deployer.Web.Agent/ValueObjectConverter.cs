using System;
using System.Linq;
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
            if (!objectType.Closes(typeof(ValueObject<,>)))
            {
                return default;
            }

            var constructors = objectType.GetConstructors();

            bool hasSingleParameterCtor = constructors.Length == 1 &&
                                          constructors.Single().GetParameters().Length == 1;

            bool hasSingleStringCtor = hasSingleParameterCtor &&
                                       constructors.Single().GetParameters().Single().ParameterType == typeof(string);
            bool hasSingleIntCtor = hasSingleParameterCtor &&
                                       constructors.Single().GetParameters().Single().ParameterType == typeof(int);

            if (existingValue is string stringValue && hasSingleStringCtor)
            {
                return Activator.CreateInstance(objectType, stringValue);
            }

            if (reader.Value is string readString && hasSingleStringCtor)
            {
                return Activator.CreateInstance(objectType, readString);
            }

            if (hasSingleIntCtor && reader.Value is int intValue)
            {
                return Activator.CreateInstance(objectType, existingValue ?? intValue);
            }

            return default;
        }

        public override bool CanConvert(Type objectType) => !objectType.IsAbstract && objectType.BaseType.Closes(typeof(ValueObject<,>));
    }
}