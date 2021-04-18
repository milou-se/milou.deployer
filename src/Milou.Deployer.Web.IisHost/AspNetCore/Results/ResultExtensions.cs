using Arbor.App.Extensions.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Milou.Deployer.Web.IisHost.AspNetCore.Results
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult(this IQueryResult? item)
        {
            if (item is null)
            {
                return new NotFoundResult();
            }

            return new ObjectResult(item);
        }

        public static IActionResult ToActionResult(this ICommandResult item)
        {
            var actionResult = new ObjectResult(item);

            return actionResult;
        }

        public static IActionResult ToActionResult(this ControllerBase controller, ICommandResult result, string? routeName = null)
        {
            var actionResult = new ObjectResult(result);

            if (!string.IsNullOrWhiteSpace(routeName)
                && controller.Url.RouteUrl(routeName) is { } routeUrl
                && !controller.Request.Headers.ContainsKey("X-Transaction-Id"))
            {
                controller.Response.Headers.Add("Location", routeUrl);
                actionResult.StatusCode = StatusCodes.Status303SeeOther;
            }

            return actionResult;
        }
    }
}