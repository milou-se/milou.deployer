using System;
using System.Collections.Generic;
using System.Linq;

namespace Milou.Deployer.Waws
{
    internal class DeploymentRuleCollection : List<DeploymentRule>
    {
        public bool TryGetValue(string name, out DeploymentRule? deploymentRule)
        {
            var found = this.SingleOrDefault(rule => rule.Name.Equals(name, StringComparison.Ordinal));

            if (found is {})
            {
                deploymentRule = found;
                return true;
            }

            deploymentRule = default;
            return false;
        }
    }
}