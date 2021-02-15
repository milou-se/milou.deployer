namespace Milou.Deployer.Web.Marten.Settings
{
    [MartenData]
    public record NexusConfigData
    {
        public string? HmacKey { get; set; }

        public string? NuGetConfig { get; set; }

        public string? NuGetSource { get; set; }
    }
}