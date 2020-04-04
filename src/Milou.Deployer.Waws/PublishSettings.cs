using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    public class PublishSettings
    {
        private const string PublishData = "publishData";
        private const string PublishProfile = "publishProfile";
        private const string PublishMethod = "publishMethod";
        private const string MSDeploy = "MSDeploy";
        private const string PublishUrl = "publishUrl";
        private const string MSDeploySite = "msdeploySite";
        private const string UserPwd = "userPWD";
        private const string UserName = "userName";

        [CanBeNull]
        public string SiteName { get; set; }

        public string ComputerName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public AuthenticationType AuthenticationType { get; set; }

        public bool AllowUntrusted { get; set; }

        public static async Task<PublishSettings> Load(string publishSettingsFile, CancellationToken cancellationToken = default)
        {
            /*
             <publishData>
    <publishProfile profileName="" publishMethod="MSDeploy" publishUrl="scm.azurewebsites.net:443" msdeploySite="" userName="$" userPWD="" destinationAppUrl="" SQLServerDBConnectionString="" mySQLDBConnectionString="" hostingProviderForumLink="" controlPanelLink="" webSystem="WebSites">
        <databases />
    </publishProfile>
             */


            XDocument document =
                await XDocument.LoadAsync(File.OpenRead(publishSettingsFile), LoadOptions.None, cancellationToken);

            XElement[] profiles = document?.Element(PublishData)
                ?.Descendants(PublishProfile).ToArray() ?? Array.Empty<XElement>();

            if (profiles.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Could not find any publish profiles in file '{publishSettingsFile}'");
            }

            XElement profile;

            if (profiles.Length > 1)
            {
                profile =
                    profiles.FirstOrDefault(
                        current => current.Attribute(PublishMethod)?.Value.Equals(MSDeploy) ?? false) ?? profiles[0];
            }
            else
            {
                profile = profiles[0];
            }

            return new PublishSettings
            {
                ComputerName = profile.Attribute(PublishUrl)?.Value,
                SiteName = profile.Attribute(MSDeploySite)?.Value,
                Username = profile.Attribute(UserName)?.Value,
                Password = profile.Attribute(UserPwd)?.Value,
                AuthenticationType = AuthenticationType.Basic
            };
        }
    }
}