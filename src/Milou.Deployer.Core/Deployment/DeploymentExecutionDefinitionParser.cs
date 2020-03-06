using System;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Milou.Deployer.Core.Deployment
{
    public static class DeploymentExecutionDefinitionParser
    {
        public static ImmutableArray<DeploymentExecutionDefinition> Deserialize([NotNull] string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(data));
            }

            try
            {
                var deploymentExecutionDefinitions = JsonConvert.DeserializeAnonymousType(
                        data,
                        new {definitions = Array.Empty<DeploymentExecutionDefinition>()}).definitions
                    .ToImmutableArray();

                return deploymentExecutionDefinitions;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not parse deployment execution definitions from data '{data}'", ex);
            }
        }
    }
}
