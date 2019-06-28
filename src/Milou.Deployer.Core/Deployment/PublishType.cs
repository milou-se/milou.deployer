using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public sealed class PublishType : IEquatable<PublishType>
    {
        public static readonly PublishType Ftp = new PublishType(nameof(Ftp));
        public static readonly PublishType Ftps = new PublishType(nameof(Ftps));
        public static readonly PublishType WebDeploy = new PublishType(nameof(WebDeploy));

        public bool IsAnyFtpType => Equals(Ftp) || Equals(Ftps);

        private PublishType([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public static PublishType Default => WebDeploy;

        public static bool TryParseOrDefault(string value, out PublishType publishType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                publishType = Default;
                return false;
            }

            var found = All.SingleOrDefault(a => a.Name.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

            publishType = found ?? Default;

            return found != null;
        }

        public bool Equals(PublishType other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is PublishType other && Equals(other);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        public static bool operator ==(PublishType left, PublishType right) => Equals(left, right);

        public static bool operator !=(PublishType left, PublishType right) => !Equals(left, right);

        public static ImmutableArray<PublishType> All => EnumerateOf<PublishType>.All;

        public override string ToString() => Name;
    }
}
