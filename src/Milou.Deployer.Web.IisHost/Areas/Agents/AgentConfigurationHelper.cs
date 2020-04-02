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


        public async Task<AgentInstallConfiguration> Handle(CreateAgentInstallConfiguration request,
            CancellationToken cancellationToken)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            byte[] bytes = Convert.FromBase64String(_authenticationConfiguration.BearerTokenIssuerKey);

            var symmetricSecurityKey = new SymmetricSecurityKey(bytes);


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.AgentName),
                new Claim(ClaimTypes.Name, request.AgentName),
            };

            SecurityTokenDescriptor securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = new DateTime(DateTime.Today.Year + 2, 12, 31, 0, 0, 0, 0),
                SigningCredentials = new SigningCredentials(symmetricSecurityKey,
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var jwtSecurityToken = handler.CreateJwtSecurityToken(securityTokenDescriptor);

            string accessToken =
                handler.WriteToken(jwtSecurityToken);

            var serverUri = new UriBuilder(_environmentConfiguration.PublicPortIsHttps ?? false ? "https" : "http",
                _environmentConfiguration.PublicHostname, _environmentConfiguration.PublicPort ?? 80);

            return new AgentInstallConfiguration(request.AgentName, accessToken, serverUri.Uri);
        }
    }
}