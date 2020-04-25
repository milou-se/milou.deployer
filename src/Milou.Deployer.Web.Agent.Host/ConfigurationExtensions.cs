using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Milou.Deployer.Web.Agent.Host.Configuration;

namespace Milou.Deployer.Web.Agent.Host
{
    public static class ConfigurationExtensions
    {
        public static string AgentId(this AgentConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.AccessToken))
            {
                throw new InvalidOperationException("Could not get agent id from configuration");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecurityToken = tokenHandler.ReadJwtToken(configuration.AccessToken);

            string claimType = JwtRegisteredClaimNames.UniqueName;
            string? agentId = jwtSecurityToken.Claims
                .SingleOrDefault(claim => claim.Type == claimType)
                ?.Value;

            if (string.IsNullOrWhiteSpace(agentId))
            {
                throw new InvalidOperationException($"The token does not contain any claim of type {claimType}");
            }

            return agentId;
        }
    }
}