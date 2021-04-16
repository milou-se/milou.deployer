namespace Milou.Deployer.Web.Marten.Settings
{
    [MartenData]
    public record DefaultNuGetConfigData
    {
        public string? NuGetConfig { get; set; }

        public string? NuGetSource { get; set; }
    }
}