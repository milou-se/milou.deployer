using System;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public sealed class ConditionalFactAttribute : FactAttribute
    {
        public ConditionalFactAttribute()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("DockerTestsEnabled"), out bool enabled) && !enabled)
            {
                Skip = "Environment variable 'DockerTestsEnabled' is set to false, skipping test";
            }
        }
    }
}