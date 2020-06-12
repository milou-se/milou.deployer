using Arbor.App.Extensions.Messaging;
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

        public static IActionResult ToActionResult(this ICommandResult item) => new ObjectResult(item);
    }
}