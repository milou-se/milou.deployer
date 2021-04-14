using System;
using AspNetCore.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Security;
using Serilog;
using Serilog.Core;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public abstract class HttpTest : IDisposable
    {
        protected readonly Logger _logger;

        protected readonly TestServer _server;


        protected HttpTest(ITestOutputHelper outputHelper)
        {
            _logger = outputHelper.CreateTestLogger();

            var webHostBuilder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IAuthorizationHandler, TestRequirementHandler>();
                    services.AddRouting();
                    services.AddControllers();
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Basic";
                    });

                    services.AddAuthentication(BasicDefaults.AuthenticationScheme)
                        .AddBasic<TestBasicUserValidationService>(options => options.Realm = "Test");

                    services.AddAuthorization(options =>
                        options.AddPolicy(AuthorizationPolicies.Agent,
                            new AuthorizationPolicy(new IAuthorizationRequirement[] {new TestRequirement()},
                                new[] {BasicDefaults.AuthenticationScheme})));
                })
                .UseStartup<Startup>();

            _server = new TestServer(webHostBuilder);
        }

        public void Dispose()
        {
            _server.Dispose();
            _logger.Dispose();
        }
    }
}