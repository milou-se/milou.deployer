namespace Milou.Deployer.Waws
{
    internal class DeploymentSyncOptions
    {
        public static DeploymentRuleCollection GetAvailableRules() => new DeploymentRuleCollection
        {
            DeploymentRule.DoNotDeleteRule
        };

        public bool DeleteDestination { get; set; }

        public DeploymentRuleCollection Rules { get; set; } = new DeploymentRuleCollection();

        public bool DoNotDelete { get; set; }

        public bool WhatIf { get; set; }

        public bool UseChecksum { get; set; } = true;
    }
}