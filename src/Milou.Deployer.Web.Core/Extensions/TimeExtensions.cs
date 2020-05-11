using System;

namespace Milou.Deployer.Web.Core.Extensions
{
    public static class TimeExtensions
    {
        public static string ToLocalDateTimeFormat(this DateTimeOffset dateTimeOffset, IAppTime appTime)
        {
            var localTime =
                TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, appTime.GetAppDefaultTimeZone());

            return localTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}