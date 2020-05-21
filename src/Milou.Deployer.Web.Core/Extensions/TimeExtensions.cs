using System;

namespace Milou.Deployer.Web.Core.Extensions
{
    public static class TimeExtensions
    {
        public static string ToLocalDateTimeFormat(this DateTimeOffset? dateTimeOffset, IAppTime appTime)
        {
            if (dateTimeOffset is null)
            {
                return "";
            }

            var localTime =
                TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.Value.UtcDateTime, appTime.GetAppDefaultTimeZone());

            return localTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}