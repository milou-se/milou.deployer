using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Milou.Deployer.Core.Cli
{
    public static class ArgExtensions
    {
        public static string GetArgumentValueOrDefault(this IEnumerable<string> args, string argumentName)
        {
            if (args is null)
            {
                return default;
            }

            string[] matchingArgs = args
                .Where(argument => argument.StartsWith("-" + argumentName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchingArgs.Length != 1)
            {
                return null;
            }

            string arg = matchingArgs[0];

            string[] parts = arg.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return null;
            }

            return parts[1];
        }
    }
}