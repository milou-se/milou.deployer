using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Milou.Deployer.Core.Deployment.WebDeploy
{
    public interface IWebDeployHelper
    {
        Task<DeploySummary> DeployContentToOneSiteAsync(
            string sourcePath,
            string? publishSettingsFile,
            TimeSpan appOfflineDelay,
            string? password = null,
            bool allowUntrusted = false,
            bool doNotDelete = true,
            TraceLevel traceLevel = TraceLevel.Off,
            bool whatIf = false,
            string? targetPath = null,
            bool useChecksum = false,
            bool appOfflineEnabled = false,
            bool appDataSkipDirectiveEnabled = false,
            bool applicationInsightsProfiler2SkipDirectiveEnabled = true,
            Action<string>? logAction = null);

        event EventHandler<CustomEventArgs> DeploymentTraceEventHandler;
    }
}