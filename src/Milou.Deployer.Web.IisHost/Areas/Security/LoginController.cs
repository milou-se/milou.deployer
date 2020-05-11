using System.Linq;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milou.Deployer.Web.IisHost.Areas.Security
{
    [Authorize]
    public class LoginController : Controller
    {
        [AllowAnonymous]
        [Route("/login")]
        [HttpGet]
        // GET
        public IActionResult Index() => View();

        [AllowAnonymous]
        [Route("/login/external")]
        [HttpGet]
        public IActionResult MakeChallenge() => Challenge(OpenIdConnectDefaults.AuthenticationScheme);

        [Route("/me")]
        [HttpGet]
        public IActionResult Me() =>
            new ObjectResult(
                new {Claims = HttpContext.User.Claims.Select(c => c.Type + " " + c.Value).ToArray()});
    }
}