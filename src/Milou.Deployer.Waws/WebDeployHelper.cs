using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.WebDeploy;
using Serilog;

namespace Milou.Deployer.Waws
{
    public class WebDeployHelper : IWebDeployHelper
    {
        private readonly ILogger _logger;

        public WebDeployHelper(ILogger logger) => _logger = logger;

        public async Task<IDeploymentChangeSummary> DeployContentToOneSiteAsync(
            string sourcePath,
            string publishSettingsFile,
            TimeSpan appOfflineDelay,
            string password = null,
            bool allowUntrusted = false,
            bool doNotDelete = true,
            TraceLevel traceLevel = TraceLevel.Off,
            bool whatIf = false,
            string targetPath = null,
            bool useChecksum = false,
            bool appOfflineEnabled = false,
            bool appDataSkipDirectiveEnabled = false,
            bool applicationInsightsProfiler2SkipDirectiveEnabled = true,
            Action<string> logAction = null)
        {
            WebDeployChangeSummary deploymentChangeSummary = await DeployContentToOneSiteAsync2(sourcePath,
                publishSettingsFile,
                appOfflineDelay,
                password,
                allowUntrusted,
                doNotDelete,
                traceLevel,
                whatIf,
                targetPath,
                useChecksum,
                appOfflineEnabled,
                appDataSkipDirectiveEnabled,
                applicationInsightsProfiler2SkipDirectiveEnabled,
                logAction
            ).ConfigureAwait(false);

            return new ResultAdapter(deploymentChangeSummary);
        }

        public event EventHandler<CustomEventArgs> DeploymentTraceEventHandler;

        private async Task<WebDeployChangeSummary> DeployContentToOneSiteAsync2(
            string sourcePath,
            string publishSettingsFile,
            TimeSpan appOfflineDelay,
            string password = null,
            bool allowUntrusted = false,
            bool doNotDelete = true,
            TraceLevel traceLevel = TraceLevel.Off,
            bool whatIf = false,
            string targetPath = null,
            bool useChecksum = false,
            bool appOfflineEnabled = false,
            bool appDataSkipDirectiveEnabled = false,
            bool applicationInsightsProfiler2SkipDirectiveEnabled = true,
            Action<string> logAction = null)
        {
            sourcePath = Path.GetFullPath(sourcePath);

            PublishSettings publishSettings = default;

            if (File.Exists(publishSettingsFile))
            {
                publishSettings = await PublishSettings.Load(publishSettingsFile);
            }

            var options = await SetBaseOptions(
                publishSettings,
                allowUntrusted);

            var destBaseOptions = options.Item1;
            string destinationPath = options.Item2;

            destBaseOptions.TraceLevel = traceLevel;
            destBaseOptions.Trace += DestBaseOptions_Trace;

            if (appDataSkipDirectiveEnabled)
            {
                destBaseOptions.SkipDirectives.Add(
                    new SkipDirective("AppData", "objectName=\"dirpath\",absolutePath=App_Data"));
            }

            if (applicationInsightsProfiler2SkipDirectiveEnabled)
            {
                destBaseOptions.SkipDirectives.Add(
                    new SkipDirective("WebJobs",
                        "objectName=\"dirpath\",absolutePath=App_Data\\\\jobs\\\\continuous"));

                destBaseOptions.SkipDirectives.Add(
                    new SkipDirective("ApplicationInsightsProfiler2",
                        "objectName=\"dirpath\",absolutePath=App_Data\\\\jobs\\\\continuous\\\\ApplicationInsightsProfiler2"));
            }

            if (!string.IsNullOrEmpty(password))
            {
                destBaseOptions.Password = password;
            }

            var sourceProvider = DeploymentWellKnownProvider.ContentPath;
            var targetProvider = DeploymentWellKnownProvider.ContentPath;

            if (!string.IsNullOrEmpty(targetPath))
            {
                if (Path.IsPathRooted(targetPath))
                {
                    sourceProvider = targetProvider = DeploymentWellKnownProvider.DirPath;

                    destinationPath = targetPath;
                }
                else
                {
                    destinationPath += "/" + targetPath;
                }
            }

            if (Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (targetProvider == DeploymentWellKnownProvider.DirPath)
                {
                    throw new DeploymentException("A source zip file can't be used with a physical target path");
                }

                sourceProvider = DeploymentWellKnownProvider.Package;
            }

            var syncOptions = new DeploymentSyncOptions
            {
                DoNotDelete = doNotDelete, WhatIf = whatIf, UseChecksum = useChecksum
            };

            logAction?.Invoke(
                $"Available deployment sync options rules:{Environment.NewLine}{string.Join(Environment.NewLine, DeploymentSyncOptions.GetAvailableRules().OrderBy(deploymentRule => deploymentRule.Name).Select(rule => $"* {rule.Name}"))}");

            logAction?.Invoke(
                $"Used deployment sync options rules:{Environment.NewLine}{string.Join(Environment.NewLine, syncOptions.Rules.OrderBy(deploymentRule => deploymentRule.Name).Select(rule => $"* {rule.Name}"))}");

            logAction?.Invoke(
                $"Using skip directives:{Environment.NewLine}{string.Join(Environment.NewLine, destBaseOptions.SkipDirectives.OrderBy(skipDirective => skipDirective.Name).Select(skipDirective => $"* {skipDirective.Name}: {skipDirective.Description}"))}");

            if (appOfflineEnabled)
            {
                const string ruleName = "AppOffline";
                bool added = AddDeploymentRule(syncOptions, ruleName);

                logAction?.Invoke(added
                    ? $"Added deployment rule '{ruleName}'"
                    : $"Could not add deployment rule '{ruleName}'");
            }

            if (!doNotDelete
                && targetProvider == DeploymentWellKnownProvider.DirPath
                && Directory.Exists(destinationPath)
                && string.IsNullOrWhiteSpace(publishSettingsFile))
            {
                var sourceDir = new DirectoryInfo(sourcePath);

                if (sourceDir.Exists)
                {
                    var targetDir = new DirectoryInfo(destinationPath);

                    FileInfo[] allTargetFiles = targetDir.GetFiles("*", SearchOption.AllDirectories);

                    FileInfo[] allSourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);

                    bool TargetExistsInSource(FileInfo targetFile)
                    {
                        string expectedSourceName = targetFile.FullName.Replace(targetDir.FullName, "");

                        bool sourceFileExists = allSourceFiles.Any(sourceFile =>
                            sourceFile.FullName.Replace(sourceDir.FullName, "").Equals(expectedSourceName,
                                StringComparison.OrdinalIgnoreCase));

                        return sourceFileExists;
                    }

                    void DeleteEmptyDirectory(DirectoryInfo currentDirectory)
                    {
                        DirectoryInfo[] subDirectories = currentDirectory.GetDirectories();

                        foreach (DirectoryInfo subDirectory in subDirectories)
                        {
                            DeleteEmptyDirectory(subDirectory);
                        }

                        currentDirectory.Refresh();

                        if (currentDirectory.GetFiles().Length == 0
                            && currentDirectory.GetDirectories().Length == 0)
                        {
                            currentDirectory.Delete();
                            logAction($"Deleted empty directory '{currentDirectory.FullName}'");
                        }
                    }

                    FileInfo[] toDelete = allTargetFiles
                        .Where(currentFile => !TargetExistsInSource(currentFile))
                        .Where(currentFile =>
                            !appDataSkipDirectiveEnabled
                            || currentFile.FullName.IndexOf("App_Data", StringComparison.OrdinalIgnoreCase) < 0)
                        .ToArray();

                    foreach (FileInfo fileInfo in toDelete)
                    {
                        fileInfo.Delete();
                        logAction($"Deleted file '{fileInfo.FullName}'");
                    }

                    DeleteEmptyDirectory(targetDir);
                }
            }

            WebDeployChangeSummary deployContentToOneSite;


            var sourceBaseOptions = publishSettings is {}
                ? await DeploymentBaseOptions.Load(publishSettings)
                : new DeploymentBaseOptions();

            using (DeploymentObject deploymentObject =
                DeploymentManager.CreateObject(sourceProvider, sourcePath, sourceBaseOptions, _logger))
            {
                FileInfo appOfflineFile = null;

                if (targetProvider == DeploymentWellKnownProvider.DirPath
                    && Directory.Exists(destinationPath)
                    && string.IsNullOrWhiteSpace(publishSettingsFile))
                {
                    string appOfflineFilePath = Path.Combine(destinationPath, DeploymentConstants.AppOfflineHtm);

                    appOfflineFile = new FileInfo(appOfflineFilePath);
                }

                if (appOfflineFile != null && appOfflineDelay.TotalMilliseconds >= 1)
                {
                    await Task.Delay(appOfflineDelay).ConfigureAwait(false);
                }

                try
                {
                    appOfflineFile?.Refresh();
                    if (appOfflineFile?.Exists == false)
                    {
                        using var _ = File.Create(appOfflineFile.FullName);
                    }

                    deployContentToOneSite = await
                        deploymentObject.SyncTo(targetProvider, destinationPath, destBaseOptions, syncOptions);
                }
                finally
                {
                    if (appOfflineFile != null)
                    {
                        appOfflineFile.Refresh();

                        if (appOfflineFile.Exists)
                        {
                            logAction($"Deleting {DeploymentConstants.AppOfflineHtm} file '{appOfflineFile.FullName}'");
                            appOfflineFile.Delete();
                            logAction($"Deleted {DeploymentConstants.AppOfflineHtm} file '{appOfflineFile.FullName}'");
                        }
                    }
                }
            }

            var result = await SetBaseOptions(publishSettings,
                allowUntrusted);

            string siteName = result.Item2;
            var destDeleteBaseOptions = result.Item1;

            var syncDeleteOptions = new DeploymentSyncOptions { DeleteDestination = true };

            if (publishSettings?.SiteName is {})
            {
                WebDeployChangeSummary results;
                using (DeploymentObject deploymentDeleteObject = DeploymentManager.CreateObject(
                    DeploymentWellKnownProvider.ContentPath,
                    siteName + "/App_Offline.htm",
                    destBaseOptions, _logger))
                {
                    destDeleteBaseOptions.TraceLevel = traceLevel;
                    destDeleteBaseOptions.Trace += DestBaseOptions_Trace;

                    results = deploymentDeleteObject.SyncTo(destDeleteBaseOptions, syncDeleteOptions);
                }

                _logger.Debug("AppOffline result: {AppOffline}", results.ToDisplayValue());
            }

            return deployContentToOneSite;
        }

        private static bool AddDeploymentRule(DeploymentSyncOptions syncOptions, string name)
        {
            DeploymentRuleCollection rules = DeploymentSyncOptions.GetAvailableRules();
            bool added = rules.TryGetValue(name, out DeploymentRule newRule);

            if (added)
            {
                syncOptions.Rules.Add(newRule);
            }

            return added;
        }

        private static async Task<(DeploymentBaseOptions, string)> SetBaseOptions(
            PublishSettings publishSettings,
            bool allowUntrusted)
        {
            if (publishSettings is {})
            {
                var deploymentBaseOptions = await DeploymentBaseOptions.Load(publishSettings);

                deploymentBaseOptions.ComputerName = publishSettings.ComputerName;
                deploymentBaseOptions.UserName = publishSettings.Username;
                deploymentBaseOptions.Password = publishSettings.Password;
                deploymentBaseOptions.AuthenticationType = publishSettings.AuthenticationType;


                deploymentBaseOptions.AllowUntrusted = allowUntrusted || publishSettings.AllowUntrusted;

                return (deploymentBaseOptions, publishSettings.SiteName);
            }

            var deploymentBaseOptions2 = new DeploymentBaseOptions();

            return (deploymentBaseOptions2, string.Empty);
        }

        private void DestBaseOptions_Trace(object sender, DeploymentTraceEventArgs e) =>
            DeploymentTraceEventHandler?.Invoke(sender, new CustomEventArgs(e.EventData, e.EventLevel, e.Message));
    }
}