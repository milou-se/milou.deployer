using System.Collections.Generic;
using System.Linq;
using System.Text;
using Milou.Deployer.Core.Logging;

namespace Milou.Deployer.ConsoleClient
{
    public static class Help
    {
        public static string ShowHelp()
        {
            var helpCommands = new Dictionary<string, string>
            {
                [ConsoleConfigurationKeys.HelpArgument] = "shows help",
                [ConsoleConfigurationKeys.DebugArgument] = "enables debugging",
                [LoggingConstants.PlainOutputFormatEnabled] =
                    $"uses format '{LoggingConstants.PlainOutputFormatEnabled}' otherwise '{LoggingConstants.DefaultFormat}'"
            };

            int longestKey = helpCommands.Keys.Select(key => key.Length).Max();

            const int extras = 3;

            string KeyWithIndentation(string key)
            {
                int lengthDiff = longestKey - key.Length;

                const char lineMarker = '.';

                if (lengthDiff > 0)
                {
                    return key + new string(lineMarker, lengthDiff + extras);
                }

                return key + new string(lineMarker, extras);
            }

            IEnumerable<string> lines = helpCommands.Keys.Select(key => KeyWithIndentation(key) + helpCommands[key]);

            var builder = new StringBuilder();

            builder.AppendLine("Help");

            foreach (string line in lines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
