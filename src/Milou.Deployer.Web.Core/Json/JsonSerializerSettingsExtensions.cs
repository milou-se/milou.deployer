using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Json
{
    [PublicAPI]
    public static class JsonSerializerSettingsExtensions
    {
        public static JsonSerializerSettings UseCustomConverters(
            [NotNull] this JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings is null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            foreach (JsonConverter customConverter in JsonConverterHelper.GetCustomConverters())
            {
                serializerSettings.Converters.Add(customConverter);
            }

            return serializerSettings;
        }

        public static JsonSerializer UseCustomConverters([NotNull] this JsonSerializer serializerSettings)
        {
            if (serializerSettings is null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            foreach (JsonConverter customConverter in JsonConverterHelper.GetCustomConverters())
            {
                serializerSettings.Converters.Add(customConverter);
            }

            return serializerSettings;
        }
    }
}