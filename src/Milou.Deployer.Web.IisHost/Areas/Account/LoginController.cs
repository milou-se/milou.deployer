using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milou.Deployer.Web.IisHost.Areas.Account
{
    [AllowAnonymous]
    [Area(nameof(Account))]
    public class LoginController : Controller
    {
        [HttpGet]
        [Route("/account/login")]
        public IActionResult Index([FromQuery] Uri? returnUrl = null)
        {
            return View();
        }
    }
}