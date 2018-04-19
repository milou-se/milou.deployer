namespace Milou.Deployer.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool ParseAsBooleanOrDefault(this string text, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return defaultValue;
            }

            if (!bool.TryParse(text, out bool parsedResultValue))
            {
                return defaultValue;
            }

            return parsedResultValue;
        }

        public static string WithDefault(this string value, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value;
        }
    }
}
