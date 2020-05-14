using System;
using JetBrains.Annotations;

namespace Milou.Deployer.Web.IisHost.Areas.Email
{
    [UsedImplicitly]
    public class EmailAddress
    {
        public EmailAddress(string address) => Address = address;

        public string Address { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Address)
                               && Address.Contains("@", StringComparison.OrdinalIgnoreCase);

        public override string ToString() => $"{nameof(Address)}: {Address}, {nameof(IsValid)}: {IsValid}";
    }
}