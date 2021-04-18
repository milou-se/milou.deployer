using System;
using System.Collections.Immutable;
using System.Linq;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Logging
{
    public static class LogEventLevelParser
    {
        private static readonly Lazy<ImmutableDictionary<string, LogEventLevel>> Lazy = new(Enum
            .GetNames(typeof(LogEventLevel))
            .Select(name =>
                (name, Enum.TryParse(name, out LogEventLevel foundLevel), foundLevel))
            .Where(level => level.Item2)
            .ToImmutableDictionary(level => level.name,
                level => level.foundLevel,
                StringComparer.OrdinalIgnoreCase));
        public static ImmutableDictionary<string, LogEventLevel> Levels => Lazy.Value;

        public static bool TryParse(string? attemptedValue, out LogEventLevel logEventLevel)
        {
            if (string.IsNullOrWhiteSpace(attemptedValue))
            {
                logEventLevel = default;
                return false;
            }

            return Levels.TryGetValue(attemptedValue, out logEventLevel);
        }
    }
}