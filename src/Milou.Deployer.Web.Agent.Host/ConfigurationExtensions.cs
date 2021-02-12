using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Milou.Deployer.Web.Agent.Host.Configuration;

namespace Milou.Deployer.Web.Agent.Host
{
    public static class ConfigurationExtensions
    {
        public static AgentId AgentId(this AgentConfiguration? configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration?.AccessToken))
            {
                throw new InvalidOperationException("There is no access token for agent configuration");
            }

            JwtSecurityToken jwtSecurityToken;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                jwtSecurityToken = tokenHandler.ReadJwtToken(configuration.AccessToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("The access token is invalid", ex);
            }

            string claimType = JwtRegisteredClaimNames.UniqueName;
            string? agentId = jwtSecurityToken.Claims
                .SingleOrDefault(claim => claim.Type == claimType)
                ?.Value;

            if (string.IsNullOrWhiteSpace(agentId))
            {
                throw new InvalidOperationException($"The token does not contain any claim of type {claimType}");
            }

            return new AgentId(agentId);
        }
    }
}