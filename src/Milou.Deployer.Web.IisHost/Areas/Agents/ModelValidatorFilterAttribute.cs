using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class ModelValidatorFilterAttribute : ActionFilterAttribute
    {
        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }

            return Task.CompletedTask;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.HttpContext.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && context.Result is null)
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}