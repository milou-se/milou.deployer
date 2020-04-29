using System;

namespace Milou.Deployer.Web.Core.Extensions
{
    public class AppTime : IAppTime
    {
        public TimeZoneInfo GetAppDefaultTimeZone() => TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); //TODO
    }
}