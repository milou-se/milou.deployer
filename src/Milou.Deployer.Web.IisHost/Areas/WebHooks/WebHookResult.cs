namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    public class WebHookResult
    {
        public WebHookResult(in bool handled) => Handled = handled;
        public bool Handled { get; }
    }
}