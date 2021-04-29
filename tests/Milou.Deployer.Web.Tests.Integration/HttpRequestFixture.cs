using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class HttpRequestFixture : WebFixtureBase, IAppHost
    {
        private readonly HttpRequestMessage _httpRequest;

        public HttpRequestFixture(IMessageSink diagnosticMessageSink, HttpRequestMessage httpRequest) :
            base(diagnosticMessageSink) => _httpRequest = httpRequest;

        public HttpResponseMessage? ResponseMessage { get; private set; }

        protected override async Task RunAsync()
        {
            using var httpClient = new HttpClient();
            var builder = new UriBuilder(_httpRequest.RequestUri);
            builder.Port = HttpPort ?? 34343;
            _httpRequest.RequestUri = builder.Uri;

            try
            {
                var response = await httpClient.SendAsync(_httpRequest, CancellationToken);

                ResponseMessage = response;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                App?.Logger.Error(ex, "Error in test when making HTTP GET request {Url}", _httpRequest.RequestUri);
                Assert.NotNull(ex);
            }
        }
    }
}