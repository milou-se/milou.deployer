using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using JetBrains.Annotations;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.IisHost.Areas.Security;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentConfigurationHelper : IRequestHandler<CreateAgentInstallConfiguration, AgentInstallConfiguration>
    {
        private readonly MilouAuthenticationConfiguration _authenticationConfiguration;
        private readonly EnvironmentConfiguration _environmentConfiguration;

        public AgentConfigurationHelper(EnvironmentConfiguration environmentConfiguration,
            MilouAuthenticationConfiguration authenticationConfiguration)
        {
            _environmentConfiguration = environmentConfiguration;
            _authenticationConfiguration = authenticationConfiguration;
        }

        public Task<AgentInstallConfiguration> Handle(CreateAgentInstallConfiguration request,
            CancellationToken cancellationToken)
        {
            var handler = new JwtSecurityTokenHandler();

            if (_authenticationConfiguration.BearerTokenIssuerKey is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(_authenticationConfiguration.BearerTokenIssuerKey)} is required");
            }

            byte[] bytes = Convert.FromBase64String(_authenticationConfiguration.BearerTokenIssuerKey);

            var symmetricSecurityKey = new SymmetricSecurityKey(bytes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, request.AgentId.Value),
                new(ClaimTypes.Name, request.AgentId.Value),
                new("milou_agent", request.AgentId.Value),
                new("unique_name", request.AgentId.Value),
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = new DateTime(DateTime.Today.Year + 2, 12, 31, 0, 0, 0, 0),
                SigningCredentials = new SigningCredentials(symmetricSecurityKey,
                    SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityToken jwtSecurityToken = handler.CreateJwtSecurityToken(securityTokenDescriptor);

            string accessToken =
                handler.WriteToken(jwtSecurityToken);

            if (string.IsNullOrWhiteSpace(_environmentConfiguration.PublicHostname))
            {
                _environmentConfiguration.PublicHostname = "localhost";
            }

            var serverUri = new UriBuilder(_environmentConfiguration.PublicPortIsHttps ?? false ? "https" : "http",
                _environmentConfiguration.PublicHostname, _environmentConfiguration.PublicPort ?? _environmentConfiguration.HttpPort ?? 80);

            return Task.FromResult(new AgentInstallConfiguration(request.AgentId, accessToken, serverUri.Uri));
        }
    }
}