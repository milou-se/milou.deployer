using System;
using System.Collections.Immutable;
using System.Linq;

namespace Milou.Deployer.Bootstrapper.Common
{
    public static class ArgExtensions
    {
        public static string GetArgumentValueOrDefault(this ImmutableArray<string> args, string argumentName)
        {
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