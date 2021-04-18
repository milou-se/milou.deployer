using System.Net.Http;
using System.Text;

namespace Milou.Deployer.Web.Tests.Integration
{
    public static class TestContent
    {
        public static readonly StringContent EmptyJson =  new("{}", Encoding.UTF8, "application/json");
    }
}