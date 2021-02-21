namespace Milou.Deployer.Web.Core.Logging
{
    public class ChangeLogLevel
    {
        public ChangeLogLevel(string? newLevel, string? timeSpan)
        {
            NewLevel = newLevel;
            TimeSpan = timeSpan;
        }

        public string? NewLevel { get; }

        public string? TimeSpan { get; }

        public override string ToString() => $"New level '{NewLevel}', timespan '{TimeSpan}'";
    }
}