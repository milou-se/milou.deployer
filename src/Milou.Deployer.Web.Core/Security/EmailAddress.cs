using System;
using System.Linq;
using System.Net.Mail;

namespace Milou.Deployer.Web.Core.Security
{
    //TODO extract from project
    public sealed class EmailAddress
    {
        private EmailAddress(string address) => Address = address;

        public string Address { get; }

        public string Domain => Address.Split('@').Last();

        public static bool TryParse(string email, out EmailAddress? emailAddress)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                emailAddress = default;
                return false;
            }

            if (!MailAddress.TryCreate(email, out var mailAddress))
            {
                emailAddress = default;
                return false;
            }

            emailAddress = new EmailAddress(mailAddress.Address);
            return true;
        }
    }
}