﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Application;
using Milou.Deployer.Tests.Integration;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class WhenMakingHttpGetRequestToRoot : TestBase<HttpGetRequestToRoot>
    {
        public WhenMakingHttpGetRequestToRoot(
            HttpGetRequestToRoot webFixture,
            ITestOutputHelper output) : base(webFixture, output)
        {
        }

        [ConditionalFact]
        public async Task Then_It_Should_Return_Html_In_Response_Body()
        {
            Assert.Null(WebFixture.Exception);
            string headers = string.Join(Environment.NewLine,
                WebFixture?.ResponseMessage?.Headers?.Select(pair => $"{pair.Key}:{string.Join(",", pair.Value)}") ??
                Array.Empty<string>());

            Output.WriteLine($"Response status: {WebFixture?.ResponseMessage?.StatusCode}");

            Output.WriteLine($"Response headers: {headers}");

            string body = WebFixture?.ResponseMessage?.Content is {}
                ? await WebFixture.ResponseMessage.Content!.ReadAsStringAsync()
                : Constants.NotAvailable;
            Output.WriteLine($"Response body: {body}");

            Assert.Contains("<html", body, StringComparison.Ordinal);
        }

        [ConditionalFact]
        public void ThenItShouldReturnHttpStatusCodeOk200()
        {
            Assert.Null(WebFixture.Exception);

            Output.WriteLine($"Response status code {WebFixture?.ResponseMessage?.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, WebFixture?.ResponseMessage?.StatusCode);
        }
    }
}