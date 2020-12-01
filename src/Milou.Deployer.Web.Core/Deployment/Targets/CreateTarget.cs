using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Core.Deployment.Messages;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public class CreateTarget : ICommand<CreateTargetResult>
    {
        public CreateTarget(string? id, string? name)
        {
            Id = id?.Trim() ?? "";
            Name = name?.Trim() ?? "";
        }

        [Required]
        public string Id { get; }

        [Required]
        public string Name { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Id)
                               && !string.IsNullOrWhiteSpace(Name)
                               && !Id.Equals(Constants.NotAvailable, StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Id ?? "[Missing Id]";
    }
}