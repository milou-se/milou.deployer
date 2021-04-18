using System;
using System.Net;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public sealed class FtpResponse : IDisposable
    {
        public FtpResponse(WebResponse webResponse)
        {
            if (webResponse is not FtpWebResponse response)
            {
                throw new InvalidOperationException("Invalid ftp response");
            }

            Response = response;
        }

        public FtpWebResponse Response { get; }

        public void Dispose() => Response.Dispose();
    }
}