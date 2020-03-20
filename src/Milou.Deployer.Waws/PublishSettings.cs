using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    internal class PublishSettings
    {
        public static async  Task<PublishSettings> Load(string publishSettingsFile)
        {
            throw new NotImplementedException();
        }

        [CanBeNull]
        public string SiteName { get; set; }

        public string ComputerName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public AuthenticationType AuthenticationType { get; set; }

        public bool AllowUntrusted { get; set; }
    }
}