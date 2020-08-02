using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Startup;

namespace Milou.Deployer.Web.IisHost.AspNetCore.Startup
{
    [UsedImplicitly]
    public class StartupTasksMiddleware
    {
        private readonly StartupTaskContext _context;
        private readonly RequestDelegate _next;
        private readonly PathString _startupSegment = new PathString("/startup");
        private static readonly PathString _hubPath = new PathString(AgentConstants.HubRoute);
        private static readonly PathString _agentTaskResultPath = new PathString(AgentConstants.DeploymentTaskResult);

        public StartupTasksMiddleware(StartupTaskContext context, RequestDelegate next)
        {
            _context = context;
            _next = next;
        }

        [PublicAPI]
        public async Task Invoke(HttpContext httpContext)
        {
            if (_context.IsCompleted
                || httpContext.Request.Path.StartsWithSegments(_startupSegment, StringComparison.OrdinalIgnoreCase)
                || httpContext.Request.Path.StartsWithSegments(_hubPath)
                || httpContext.Request.Path.StartsWithSegments(_agentTaskResultPath)
                || httpContext.Request.Path.Value.StartsWith("/deployment-task"))
            {
                await _next(httpContext);
            }
            else
            {
                HttpResponse response = httpContext.Response;

                //if ()
                //{
                //    response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                //    return;
                //}

                response.StatusCode = (int)HttpStatusCode.TemporaryRedirect;

                response.Headers.TryAdd("location", _startupSegment.Value);
            }
        }
    }
}