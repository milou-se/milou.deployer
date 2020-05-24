using System.ComponentModel.DataAnnotations;
using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public sealed class CreateOrganization : ICommand<CreateOrganizationResult>
    {
        public CreateOrganization(string id) => Id = id;

        [Required]
        public string Id { get; }

        public bool IsValid => Id is {};
    }
}