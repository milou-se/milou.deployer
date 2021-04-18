using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using Arbor.AspNetCore.Host.Hosting;
using Arbor.AspNetCore.Host.Mvc;
using Arbor.AspNetCore.Mvc.Formatting.HtmlForms;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Core.Extensions.BoolExtensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Milou.Deployer.Web.Core.Json;
using Milou.Deployer.Web.Core.Security;
using Milou.Deployer.Web.IisHost.Areas.Security;
using Milou.Deployer.Web.IisHost.Areas.Startup;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using MessageReceivedContext = Microsoft.AspNetCore.Authentication.JwtBearer.MessageReceivedContext;
using TokenValidatedContext = Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext;

namespace Milou.Deployer.Web.IisHost.AspNetCore.Startup
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddDeploymentAuthentication(
            this IServiceCollection serviceCollection,
            CustomOpenIdConnectConfiguration? openIdConnectConfiguration,
            MilouAuthenticationConfiguration? milouAuthenticationConfiguration,
            ILogger logger,
            EnvironmentConfiguration environmentConfiguration)
        {
            AuthenticationBuilder authenticationBuilder = serviceCollection
                .AddAuthentication(
                    option =>
                    {
                        option.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                        if (openIdConnectConfiguration?.Enabled == true)
                        {
                            option.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                        }
                        else
                        {
                            option.DefaultAuthenticateScheme = MilouAuthenticationConstants.MilouAuthenticationScheme;
                        }
                    })
                .AddCookie();

            if (openIdConnectConfiguration?.Enabled == true)
            {
                authenticationBuilder = authenticationBuilder.AddOpenIdConnect(
                    openIdConnectOptions =>
                    {
                        openIdConnectOptions.ClientId = openIdConnectConfiguration.ClientId;
                        openIdConnectOptions.ClientSecret = openIdConnectConfiguration.ClientSecret;
                        openIdConnectOptions.Authority = openIdConnectConfiguration.Authority;
                        openIdConnectOptions.ResponseType = "code";
                        openIdConnectOptions.GetClaimsFromUserInfoEndpoint = true;
                        openIdConnectOptions.MetadataAddress = openIdConnectConfiguration.MetadataAddress;
                        openIdConnectOptions.Scope.Add("email");
                        openIdConnectOptions.TokenValidationParameters.ValidIssuer = openIdConnectConfiguration.Issuer;

                        openIdConnectOptions.TokenValidationParameters.IssuerValidator = (issuer, _, __) =>
                        {
                            if (string.Equals(issuer, openIdConnectConfiguration.Issuer, StringComparison.Ordinal))
                            {
                                return issuer;
                            }

                            throw new InvalidOperationException("Invalid issuer");
                        };

                        openIdConnectOptions.Events.OnRemoteFailure = context =>
                        {
                            logger.Error(context.Failure, "Remote call to OpenIDConnect {Uri} failed",
                                context.Options.Backchannel.BaseAddress);

                            return Task.CompletedTask;
                        };

                        openIdConnectOptions.Events.OnRedirectToIdentityProvider = context =>
                        {
                            var redirectUrl = new Uri("http://localhost/signin-oidc");

                            var builder = new UriBuilder(redirectUrl);

                            if (!string.IsNullOrWhiteSpace(environmentConfiguration.PublicHostname))
                            {
                                builder.Host = environmentConfiguration.PublicHostname;

                                if (logger.IsEnabled(LogEventLevel.Verbose))
                                {
                                    logger.Verbose("Using redirect from environment public host name {HostName}", environmentConfiguration.PublicHostname);
                                }
                            }
                            else if (logger.IsEnabled(LogEventLevel.Verbose))
                            {
                                logger.Verbose("Using default redirect for OpenId Connect");
                            }

                            if (environmentConfiguration.PublicPortIsHttps == true)
                            {
                                builder.Scheme = "https";
                                builder.Port = environmentConfiguration.HttpsPort ?? 443;
                            }

                            context.ProtocolMessage.RedirectUri = builder.Uri.AbsoluteUri;

                            return Task.CompletedTask;
                        };
                    });
            }

            if (milouAuthenticationConfiguration?.Enabled == true)
            {
                authenticationBuilder.AddMilouAuthentication(
                    MilouAuthenticationConstants.MilouAuthenticationScheme,
                    "Milou", _ => { });
            }

            if (milouAuthenticationConfiguration?.BearerTokenEnabled == true
                && !string.IsNullOrWhiteSpace(milouAuthenticationConfiguration.BearerTokenIssuerKey))
            {
                logger.Information("Bearer token authentication is enabled");

                authenticationBuilder.AddJwtBearer(options =>
                {
                    byte[] bytes = Convert.FromBase64String(milouAuthenticationConfiguration.BearerTokenIssuerKey);

                    options.TokenValidationParameters = new ()
                    {
                        IssuerSigningKeys = new List<SecurityKey> {new SymmetricSecurityKey(bytes)},
                        ValidateAudience = false,
                        ValidateIssuer = false
                    };

                    options.Events = new ()
                    {
                        OnMessageReceived = OnMessageReceived,
                        OnChallenge = OnChallenge,
                        OnTokenValidated = OnTokenValidated
                    };
                });
            }
            else
            {
                logger.Information("Bearer token authentication is disabled");
            }

            return serviceCollection;
        }

        private static Task OnChallenge(JwtBearerChallengeContext arg) => Task.CompletedTask;

        private static Task OnTokenValidated(TokenValidatedContext arg) => Task.CompletedTask;

        private static Task OnMessageReceived(MessageReceivedContext arg) => Task.CompletedTask;

        public static IServiceCollection AddDeploymentMvc(this IServiceCollection services,
            EnvironmentConfiguration environmentConfiguration,
            IKeyValueConfiguration configuration,
            ILogger logger,
            IApplicationAssemblyResolver applicationAssemblyResolver)
        {
            IMvcBuilder mvcBuilder = services.AddMvc(
                    options =>
                    {
                        options.InputFormatters.Insert(0, new XWwwFormUrlEncodedFormatter());
                        options.Filters.Add<ModelValidatorFilterAttribute>();
                    }).SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddNewtonsoftJson(
                    options =>
                    {
                        options.SerializerSettings.Converters.Add(new DateConverter());
                        options.SerializerSettings.Formatting = Formatting.Indented;
                    });

            var filteredAssemblies = applicationAssemblyResolver.GetAssemblies();

            foreach (Assembly filteredAssembly in filteredAssemblies)
            {
                logger.Debug("Adding assembly {Assembly} to MVC application parts", filteredAssembly.FullName);
                mvcBuilder.AddApplicationPart(filteredAssembly);
            }

            services.AddControllers()
                .AddControllersAsServices();

            IMvcBuilder razorPagesBuilder = services.AddRazorPages();

#if DEBUG
            if (environmentConfiguration.ToHostEnvironment().IsDevelopment()
                || configuration.ValueOrDefault(StartupConstants.RuntimeCompilationEnabled))
            {
                razorPagesBuilder.AddRazorRuntimeCompilation();
            }
#endif

            return services;
        }

        public static IServiceCollection AddServerFeatures(this IServiceCollection services)
        {
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IServerAddressesFeature, ServerAddressesFeature>();

            return services;
        }

        public static IServiceCollection AddDeploymentAuthorization(
            this IServiceCollection services,
            EnvironmentConfiguration environmentConfiguration)
        {
            services.AddAuthorization(
                options =>
                {
                    options.AddPolicy(
                        AuthorizationPolicies.IpOrToken,
                        policy => policy.Requirements.Add(new DefaultAuthorizationRequirement()));

                    options.AddPolicy(
                        AuthorizationPolicies.Agent,
                        policy =>
                        {
                            policy.Requirements.Add(new AgentAuthorizationRequirement());
                            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                        });
                });

            services.AddSingleton<IAuthorizationHandler, DefaultAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, AgentAuthorizationHandler>();

            if (environmentConfiguration.IsDevelopmentMode)
            {
                services.AddSingleton<IAuthorizationHandler, DevelopmentPermissionHandler>();
            }

            return services;
        }

        public static IServiceCollection AddDeploymentSignalR(this IServiceCollection services)
        {
            services.AddSignalR(
                options =>
                {
                    options.EnableDetailedErrors = true;
                    options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                });

            return services;
        }
    }
}