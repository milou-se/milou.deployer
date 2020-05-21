﻿using System.ComponentModel.DataAnnotations;
using Arbor.App.Extensions;
using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public sealed class CreateOrganization : ICommand<CreateOrganizationResult>
    {
        public CreateOrganization(string id) => Id = id;

        [Required]
        public string Id { get; }

        public bool IsValid => Id.HasValue();
    }
}