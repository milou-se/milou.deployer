using Arbor.KVConfiguration.Core.Metadata;

namespace Milou.Deployer.Core.Cli
{
    public static class ConsoleConfigurationKeys
    {
        [Metadata]
        public const string LoggingFilePath = "urn:milou-deployer:logging:log-file-path";

        public const string HelpArgument = "--help";

        public const string DebugArgument = "--debug";

        public const string NonInteractiveArgument = "--non-interactive";
    }
}