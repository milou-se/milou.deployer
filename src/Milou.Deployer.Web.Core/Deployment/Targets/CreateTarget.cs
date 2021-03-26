using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment.Messages;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class CreateTarget : ICommand<CreateTargetResult>
    {
        public CreateTarget(string? id, string? name)
        {
            Id = string.IsNullOrWhiteSpace(id) ? DeploymentTargetId.Invalid : new DeploymentTargetId(id);
            Name = name?.Trim() ?? "";
        }

        [Required]
        public DeploymentTargetId Id { get; }

        [Required]
        public string Name { get; }

        public bool IsValid => Id != DeploymentTargetId.Invalid
                               && !string.IsNullOrWhiteSpace(Name);

        public override string ToString() => Id == DeploymentTargetId.Invalid ? "[Missing Id]" : Id.TargetId;
    }
}