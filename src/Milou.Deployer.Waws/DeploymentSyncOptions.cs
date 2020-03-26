using System;

namespace Milou.Deployer.Waws
{
    internal class DeploymentSyncOptions
    {
        public static DeploymentRuleCollection GetAvailableRules()
        {
            return new DeploymentRuleCollection(); //TODO
        }

        public bool DeleteDestination { get; set; }

        public DeploymentRuleCollection Rules { get; set; } = new DeploymentRuleCollection();

        public bool DoNotDelete { get; set; }

        public bool WhatIf { get; set; }

        public bool UseChecksum { get; set; } = true;
    }
}