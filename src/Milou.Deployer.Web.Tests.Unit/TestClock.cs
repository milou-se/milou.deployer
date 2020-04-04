﻿using System;
using Arbor.App.Extensions.Time;

namespace Milou.Deployer.Web.Tests.Unit
{
    internal class TestClock : ICustomClock
    {
        public DateTimeOffset UtcNow() => new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        public DateTime LocalNow() => new DateTime(2000, 1, 2, 4, 4, 5, DateTimeKind.Local);

        public DateTime ToLocalTime(DateTime dateTimeUtc)
        {
            DateTime localTime = dateTimeUtc.AddHours(-1);

            return new DateTime(localTime.Ticks, DateTimeKind.Local);
        }
    }
}
