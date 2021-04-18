using System;
using System.Net;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public class FtpException : Exception
    {
        public FtpException(string message) : base(message)
        {
        }

        public FtpException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public FtpException(string message, FtpStatusCode responseStatusCode) : base(
            $"{message}, status code {responseStatusCode}")
        {
        }
    }
}