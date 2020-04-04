using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.Core.Deployment
{
    public sealed class PublishType : IEquatable<PublishType>
    {
        public static readonly PublishType Ftp = new PublishType(nameof(Ftp));
        public static readonly PublishType Ftps = new PublishType(nameof(Ftps));
        public static readonly PublishType WebDeploy = new PublishType(nameof(WebDeploy));

        private PublishType([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(name));
            }

            Name = name;
        }

        public static PublishType Default => WebDeploy;

        public static ImmutableArray<PublishType> All => EnumerateOf<PublishType>.All;

        public bool IsAnyFtpType => Equals(Ftp) || Equals(Ftps);

        public string Name { get; }

        public static bool TryParseOrDefault(string value, out PublishType? publishType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                publishType = Default;
                return false;
            }

            PublishType found = All.SingleOrDefault(a => a.Name.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

            publishType = found ?? Default;

            return found is {};
        }

        public static bool operator ==(PublishType left, PublishType right) => Equals(left, right);

        public static bool operator !=(PublishType left, PublishType right) => !Equals(left, right);

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

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is PublishType other && Equals(other));

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        public override string ToString() => Name;
    }
}
