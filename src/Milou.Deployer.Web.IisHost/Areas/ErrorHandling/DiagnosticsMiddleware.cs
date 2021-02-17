using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Milou.Deployer.Web.IisHost.Areas.ErrorHandling
{
    public class DiagnosticsMiddleware
    {
        private readonly RequestDelegate _next;

        public DiagnosticsMiddleware(RequestDelegate next) => _next = next;

        public Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            if (endpoint is {})
            {
                // TODO
            }

            return _next(context);
        }
    }
}