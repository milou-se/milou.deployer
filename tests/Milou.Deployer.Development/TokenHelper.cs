using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Development
{
    public static class TokenHelper
    {
        public static byte[] GenerateKey()
        {
            using var hmac = new HMACSHA256();
            byte[] keyBytes = hmac.Key;

            return keyBytes;
        }

        public static string GenerateToken(AgentId agentId, byte[] keyBytes)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, agentId.Value),
                new(ClaimTypes.Name, agentId.Value),
                new("milou_agent", agentId.Value)
            };

            var handler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = new DateTime(DateTime.Today.Year + 2, 12, 31, 0, 0, 0, 0),
                SigningCredentials = new SigningCredentials(securityKey,
                    SecurityAlgorithms.HmacSha256Signature)
            };

            IdentityModelEventSource.ShowPII = true;

            JwtSecurityToken securityToken = handler.CreateJwtSecurityToken(tokenDescriptor);
            string jwt = handler.WriteToken(securityToken);

            return jwt;
        }
    }
}