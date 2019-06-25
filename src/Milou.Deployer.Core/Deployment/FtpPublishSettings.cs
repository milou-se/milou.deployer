using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpPublishSettings
    {
        private FtpPublishSettings(string userName, string password, Uri ftpBaseUri)
        {
            UserName = userName;
            Password = password;
            FtpBaseUri = ftpBaseUri;
        }

        public string UserName { get; }

        public string Password { get; }

        public Uri FtpBaseUri { get; }

        public static FtpPublishSettings Load(string publishSettingsFile)
        {
            using (var fileStream = new FileStream(publishSettingsFile, FileMode.Open))
            {
                XDocument document = XDocument.Load(fileStream);

                IEnumerable<XElement> descendantNodes =
                    document.Element("publishData")?.Descendants("publishProfile") ??
                    throw new InvalidOperationException("Missing publishData and publishProfiles");

                XElement ftpElement = descendantNodes.SingleOrDefault(element =>
                {
                    string ftpAttribute = element.Attribute("publishMethod")?.Value;

                    if (ftpAttribute is null)
                    {
                        return false;
                    }

                    if (ftpAttribute.Equals("FTP", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                });

                if (ftpElement is null)
                {
                    throw new InvalidOperationException("Could not find element with publishMethod FTP");
                }

                string userName = ftpElement.Attribute("userName")?.Value;
                string password = ftpElement.Attribute("userPWD")?.Value;
                string ftpBaseUri = ftpElement.Attribute("publishUrl")?.Value;

                if (string.IsNullOrWhiteSpace(userName))
                {
                    throw new InvalidOperationException("Missing userName in publish settings");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("Missing userPWD in publish settings");
                }

                if (string.IsNullOrWhiteSpace(ftpBaseUri))
                {
                    throw new InvalidOperationException("Missing publishUrl in publish settings");
                }

                if (!Uri.TryCreate(ftpBaseUri, UriKind.Absolute, out Uri uri))
                {
                    throw new InvalidOperationException($"The ftp uri '{ftpBaseUri}' is not valid");
                }

                if (!uri.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid scheme for ftp URI {uri.AbsoluteUri}");
                }

                return new FtpPublishSettings(userName, password, uri);
            }
        }
    }
}
