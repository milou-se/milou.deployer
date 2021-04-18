﻿namespace Milou.Deployer.Web.Agent
{
    public static class AgentConstants
    {
        public const string HubRoute = "/agents/hub";

        public const string SignalRDeployCommand = "Deploy";

        public const string SignalRPingCommand = "Ping";

        public const string DeploymentTaskResult = "/deployment-task/result";

        public const string DeploymentTaskResultName = nameof(DeploymentTaskResult);

        public const string DeploymentTaskPackageRoute = "/deployment-task-package/{deploymentTaskId}";

        public const string DeploymentTaskPackageRouteName = nameof(DeploymentTaskPackageRoute);

        public const string DeploymentTaskLogRoute = "/deployment-task/log";

        public const string DeploymentTaskLogRouteName = nameof(DeploymentTaskLogRoute);
    }
}