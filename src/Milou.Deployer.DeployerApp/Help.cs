using System.Collections.Generic;
using System.Linq;
using System.Text;
using Milou.Deployer.Core.Cli;
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
                [ConsoleConfigurationKeys.NonInteractiveArgument] = "disables interactive user prompts, default is interactive if the user session is interactive",
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

            builder.AppendLine("Example:");
            builder.AppendLine(@"Milou.Deployer.ConsoleClient.exe C:\data\manifest.json");

            foreach (string line in lines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
