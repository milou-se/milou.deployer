namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Controllers
{
    public class InvalidateCache
    {
        public string? Prefix { get; }

        public InvalidateCache(string? prefix)
        {
            Prefix = prefix;
        }
    }
}