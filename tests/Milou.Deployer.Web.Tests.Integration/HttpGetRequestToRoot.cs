using System.Net.Http;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class HttpGetRequestToRoot : HttpRequestFixture
    {
        public HttpGetRequestToRoot([NotNull] IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink, CreateRequest())
        {
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new(HttpMethod.Get, "http://localhost");
        }
    }
}