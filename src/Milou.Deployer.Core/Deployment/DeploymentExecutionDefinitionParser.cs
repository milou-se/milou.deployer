using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Milou.Deployer.Core.Deployment
{
    public static class DeploymentExecutionDefinitionParser
    {
        public static ImmutableArray<DeploymentExecutionDefinition> Deserialize(string data)
        {
            return JsonConvert.DeserializeAnonymousType(
                data,
                new
                {
                    definitions = System.Array.Empty<DeploymentExecutionDefinition>()
                }).definitions.ToImmutableArray();
        }
    }
}
