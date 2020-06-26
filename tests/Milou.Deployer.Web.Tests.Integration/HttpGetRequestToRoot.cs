using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class HttpGetRequestToRoot : WebFixtureBase, IAppHost
    {
        public HttpGetRequestToRoot(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }

        public HttpResponseMessage ResponseMessage { get; private set; }

        protected override async Task RunAsync()
        {
            using var httpClient = new HttpClient();
            string url = $"http://localhost:{HttpPort}";

            while (!CancellationToken.IsCancellationRequested)
            {
                var response = await httpClient.GetAsync(url, CancellationToken);

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(3000));
            }

            try
            {
                ResponseMessage = await httpClient.GetAsync(url, CancellationToken);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                App?.Logger?.Error(ex, "Error in test when making HTTP GET request {Url}", url);
                Assert.NotNull(ex);
            }
        }
    }
}