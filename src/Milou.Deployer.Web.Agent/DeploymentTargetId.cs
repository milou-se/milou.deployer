using System;
using Arbor.App.Extensions;
using JetBrains.Annotations;

namespace Milou.Deployer.Web.Agent
{
    public class DeploymentTargetId : ValueObject<DeploymentTargetId, string>
    {
        public DeploymentTargetId([NotNull] string targetId) : base(targetId)
        {
        }

        protected override void Validate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", "targetId");
            }
        }

        public string TargetId => Value;

        public static readonly DeploymentTargetId Invalid = new(Constants.NotAvailable);

        public override string ToString() => TargetId;

        public static bool TryParse(string? value, out DeploymentTargetId? deploymentTargetId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                deploymentTargetId = default;
                return false;
            }

            deploymentTargetId = new(value);
            return true;
        }
    }
}