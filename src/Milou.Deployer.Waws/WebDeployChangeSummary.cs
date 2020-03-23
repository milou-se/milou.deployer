namespace Milou.Deployer.Waws
{
    public class WebDeployChangeSummary
    {
        public long AddedFiles { get; set; }
        public long AddedDirectories { get; set; }
        public long DeletedFiles { get; set; }
        public long DeletedDirectories { get; set; }
        public int ExitCode { get; set; }
    }
}