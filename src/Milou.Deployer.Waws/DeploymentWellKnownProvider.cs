using System;

namespace Milou.Deployer.Waws
{
    internal sealed class DeploymentWellKnownProvider : IEquatable<DeploymentWellKnownProvider>
    {
        public static readonly DeploymentWellKnownProvider ContentPath =
            new DeploymentWellKnownProvider(nameof(ContentPath));

        public static readonly DeploymentWellKnownProvider Package = new DeploymentWellKnownProvider(nameof(Package));

        public static readonly DeploymentWellKnownProvider DirPath = new DeploymentWellKnownProvider(nameof(DirPath));

        public DeploymentWellKnownProvider(string name) => Name = name;

        public string Name { get; }

        public bool Equals(DeploymentWellKnownProvider other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name;
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || (obj is DeploymentWellKnownProvider other && Equals(other));

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(DeploymentWellKnownProvider left, DeploymentWellKnownProvider right) =>
            Equals(left, right);

        public static bool operator !=(DeploymentWellKnownProvider left, DeploymentWellKnownProvider right) =>
            !Equals(left, right);

        public override string ToString() => Name;
    }
}