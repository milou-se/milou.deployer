using System;
using System.Net;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpRequest
    {
        public FtpRequest([NotNull] FtpWebRequest request, [NotNull] FtpMethod method)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public FtpWebRequest Request { get; }

        public FtpMethod Method { get; }

        public Uri RequestUri => Request.RequestUri;
    }
}
