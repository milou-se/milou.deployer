﻿using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentConfigurationController : BaseApiController
    {
        [AllowAnonymous]
        [HttpPost]
        [Route("~/agent/install-configuration/")]
        public async Task<IActionResult> Index([FromBody]CreateAgentInstallConfiguration createAgentInstallConfiguration,
            [FromServices] IMediator mediator)
        {
            AgentInstallConfiguration agentInstallConfiguration = await mediator.Send(createAgentInstallConfiguration);

            return new ObjectResult(agentInstallConfiguration);
        }
    }
}