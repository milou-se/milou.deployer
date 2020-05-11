﻿using Serilog;

namespace Milou.Deployer.Waws
{
    internal static class DeploymentManager
    {
        public static DeploymentObject CreateObject(DeploymentWellKnownProvider provider,
            string path,
            DeploymentBaseOptions deploymentBaseOptions,
            ILogger logger) =>
            new DeploymentObject(provider, path, deploymentBaseOptions, logger);
    }
}