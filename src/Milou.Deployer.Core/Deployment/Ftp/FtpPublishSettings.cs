using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public class FtpPublishSettings
    {
        private const string UsernameAttribute = "userName";
        private const string UserPasswordAttribute = "userPWD";
        private const string PublishUrlAttribute = "publishUrl";
        private const string PublishMethodAttribute = "publishMethod";

        private FtpPublishSettings(string userName, string password, [NotNull] Uri ftpBaseUri)
        {
            UserName = userName;
            Password = password;
            FtpBaseUri = ftpBaseUri ?? throw new ArgumentNullException(nameof(ftpBaseUri));
        }

        public string UserName { get; }

        public string Password { get; }

        public Uri FtpBaseUri { get; }

        public static FtpPublishSettings Load(string publishSettingsFile)
        {
            using var fileStream = new FileStream(publishSettingsFile, FileMode.Open);
            var document = XDocument.Load(fileStream);

            IEnumerable<XElement> descendantNodes =
                document.Element("publishData")?
                    .Descendants("publishProfile") ??
                throw new InvalidOperationException("Missing publishData and publishProfiles");

            XElement ftpElement = descendantNodes.SingleOrDefault(element =>
            {
                string? ftpAttribute = element.Attribute(PublishMethodAttribute)?.Value;

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

            string? userName = ftpElement.Attribute(UsernameAttribute)?.Value;
            string? password = ftpElement.Attribute(UserPasswordAttribute)?.Value;
            string? ftpBaseUri = ftpElement.Attribute(PublishUrlAttribute)?.Value;

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new InvalidOperationException($"Missing {UsernameAttribute} in publish settings");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException($"Missing {UserPasswordAttribute} in publish settings");
            }

            if (string.IsNullOrWhiteSpace(ftpBaseUri))
            {
                throw new InvalidOperationException($"Missing {PublishUrlAttribute} in publish settings");
            }

            if (!Uri.TryCreate(ftpBaseUri, UriKind.Absolute, out var uri))
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