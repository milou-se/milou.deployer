using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using Milou.Deployer.Core.Deployment.Configuration;

namespace Milou.Deployer.Core.Deployment
{
    public static class DeploymentExecutionDefinitionExtensions
    {
        public static bool WhatIfEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = false)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.WhatIfEnabled);
        }

        public static bool DoNotDeleteEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = true)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.DoNotDeleteEnabled);
        }

        public static bool AppOfflineEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = true)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.AppOfflineEnabled);
        }

        public static bool UseChecksumEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = false)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.UseChecksumEnabled);
        }

        public static bool AppDataSkipDirectiveEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = false)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.AppDataSkipDirectiveEnabled);
        }

        public static bool ApplicationInsightsProfiler2SkipDirectiveEnabled(
            [NotNull] this DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue = true)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            return GetBoolValue(deploymentExecutionDefinition,
                defaultValue,
                WebDeployRules.ApplicationInsightsProfiler2SkipDirectiveEnabled);
        }

        private static bool GetBoolValue(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            bool defaultValue,
            string configurationKey)
        {
            deploymentExecutionDefinition.Parameters.TryGetValue(configurationKey,
                out StringValues values);

            if (values.Count == 1 && bool.TryParse(values[0], out bool flag))
            {
                return flag;
            }

            return defaultValue;
        }
    }
}
