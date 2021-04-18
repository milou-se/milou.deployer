namespace Milou.Deployer.Waws
{
    internal class DeploymentSyncOptions
    {
        public bool DeleteDestination { get; set; }

        public DeploymentRuleCollection Rules { get; set; } = new();

        public bool DoNotDelete { get; set; }

        public bool WhatIf { get; set; }

        public bool UseChecksum { get; set; } = true;

        public static DeploymentRuleCollection GetAvailableRules() => new()
        {
            DeploymentRule.DoNotDeleteRule
        };
    }
}