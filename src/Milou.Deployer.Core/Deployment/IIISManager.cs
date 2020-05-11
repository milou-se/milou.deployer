using System;

namespace Milou.Deployer.Core.Deployment
{
    public interface IIisManager : IDisposable
    {
        bool StopSiteIfApplicable();
    }
}