using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Milou.Deployer.Web.Tests.Integration
{
    public static class TestAuthenticationExtensions
    {
        public static void AddTestBasicAuthentication(this HttpClient client) =>
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("test:test")));
    }
}