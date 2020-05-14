using System;
using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    public sealed class AuthenticationType : IEquatable<AuthenticationType>
    {
        public static readonly AuthenticationType Basic = new AuthenticationType(nameof(Basic));

        private AuthenticationType([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public bool Equals(AuthenticationType other)
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
            ReferenceEquals(this, obj) || (obj is AuthenticationType other && Equals(other));

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public static bool operator ==(AuthenticationType left, AuthenticationType right) => Equals(left, right);

        public static bool operator !=(AuthenticationType left, AuthenticationType right) => !Equals(left, right);
    }
}