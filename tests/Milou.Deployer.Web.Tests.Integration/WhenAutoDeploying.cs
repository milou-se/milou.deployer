using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.KVConfiguration.JsonConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Tests.Integration;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Startup;
using Milou.Deployer.Web.IisHost.AspNetCore.Startup;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class WhenAutoDeploying : TestBase<AutoDeploySetup>
    {
        public WhenAutoDeploying(
            ITestOutputHelper output,
            AutoDeploySetup webFixture) : base(webFixture, output)
        {
        }

        //[Fact(Skip = "NuGet source issues")]
        [NCrunch.Framework.Timeout(120_000)]
        [ConditionalFact]
        public async Task ThenNewVersionShouldBeDeployed()
        {
            SemanticVersion? semanticVersion = null;

            var expectedVersion = new SemanticVersion(1, 2, 5);

            if (WebFixture is null)
            {
                throw new DeployerAppException($"{nameof(WebFixture)} is null");
            }

            if (WebFixture.ServerEnvironmentTestSiteConfiguration is null)
            {
                throw new DeployerAppException($"{nameof(WebFixture.ServerEnvironmentTestSiteConfiguration)} is null");
            }

            if (WebFixture is null)
            {
                throw new DeployerAppException($"{nameof(WebFixture)} is null");
            }

            Output.WriteLine(typeof(StartupModule).FullName);

            Assert.NotNull(WebFixture?.App?.Host?.Services);

            using (var httpClient = WebFixture!.App!.Host!.Services.GetRequiredService<IHttpClientFactory>().CreateClient())
            {
                using CancellationTokenSource cancellationTokenSource =
                    WebFixture!.App!.Host!.Services.GetRequiredService<CancellationTokenSource>();

                var lifeTime = WebFixture!.App!.Host!.Services.GetRequiredService<IHostApplicationLifetime>();

                cancellationTokenSource.Token.Register(() => Debug.WriteLine("Cancellation for app in test"));

                lifeTime.ApplicationStopped.Register(() => Debug.WriteLine("Stop for app in test"));

                while (!cancellationTokenSource.Token.IsCancellationRequested
                       && semanticVersion != expectedVersion
                       && !lifeTime.ApplicationStopped.IsCancellationRequested
                       && !WebFixture!.CancellationToken.IsCancellationRequested)
                {
                    // ReSharper disable MethodSupportsCancellation
                    StartupTaskContext? startupTaskContext =
                        WebFixture!.App!.Host!.Services.GetService<StartupTaskContext>();

                    if (startupTaskContext is null)
                    {
                        return;
                    }

                    while (!startupTaskContext.IsCompleted &&
                           !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }

                    var url = new Uri(
                        $"http://localhost:{WebFixture!.ServerEnvironmentTestSiteConfiguration.Port.Port + 1}/applicationmetadata.json");

                    string contents;
                    try
                    {
                        using HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
                        contents = await responseMessage.Content.ReadAsStringAsync();

                        Output.WriteLine($"{responseMessage.StatusCode} {contents}");

                        if (responseMessage.StatusCode == HttpStatusCode.ServiceUnavailable
                            || responseMessage.StatusCode == HttpStatusCode.NotFound)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                            continue;
                        }

                        Assert.Equal(HttpStatusCode.OK, responseMessage.StatusCode);
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        throw new DeployerAppException($"Could not get a valid response from request to '{url}'",
                            ex);
                    }

                    string tempFileName = Path.GetTempFileName();
                    await File.WriteAllTextAsync(tempFileName,
                        contents,
                        Encoding.UTF8,
                        cancellationTokenSource.Token);

                    var jsonKeyValueConfiguration =
                        new JsonKeyValueConfiguration(tempFileName);

                    if (File.Exists(tempFileName))
                    {
                        File.Delete(tempFileName);
                    }

                    string actual = jsonKeyValueConfiguration["urn:versioning:semver2:normalized"];

                    semanticVersion = SemanticVersion.Parse(actual);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    // ReSharper restore MethodSupportsCancellation
                }
            }

            if (WebFixture?.Exception is { } exception)
            {
                throw new DeployerAppException("Fixture exception", exception);
            }

            Assert.Equal(expectedVersion, semanticVersion);
        }
    }
}