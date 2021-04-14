using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestRequirementHandler : AuthorizationHandler<TestRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TestRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated == true && string.Equals("test", context.User.Identity.Name))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}