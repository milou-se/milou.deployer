using System;

namespace Milou.Deployer.Web.Core.Extensions
{
    public interface IAppTime
    {
        TimeZoneInfo GetAppDefaultTimeZone();
    }
}