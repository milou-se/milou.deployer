using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Milou.Deployer.Web.IisHost.Areas.Security
{
    [UsedImplicitly]
    public class AgentAuthorizationHandler : AuthorizationHandler<AgentAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            AgentAuthorizationRequirement requirement)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                return Task.CompletedTask;
            }

            context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}