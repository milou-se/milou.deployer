using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Milou.Deployer.Waws
{
    public static class DeploymentChangeSummaryExtensions
    {
        public static string ToDisplayValue([NotNull] this WebDeployChangeSummary summary)
        {
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            return JsonConvert.SerializeObject(summary, Formatting.Indented);
        }
    }
}