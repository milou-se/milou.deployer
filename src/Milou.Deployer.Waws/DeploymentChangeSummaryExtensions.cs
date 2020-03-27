using System;
using JetBrains.Annotations;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;
using Newtonsoft.Json;

namespace Milou.Deployer.Waws
{
    public static class DeploymentChangeSummaryExtensions
    {
        public static string ToDisplayValue([NotNull] this DeploySummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            return JsonConvert.SerializeObject(summary, Formatting.Indented);
        }
    }
}