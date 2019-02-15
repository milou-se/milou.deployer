using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Web.Deployment;
using Milou.Deployer.Core.Deployment;

namespace Milou.Deployer.Waws
{
    public class WebDeployHelper : IWebDeployHelper
    {
        private const string AppOfflineHtm = "App_Offline.htm";

        /// <summary>
        /// Deploys the content to one site.
        /// </summary>
        /// <param name="sourcePath">The content path.</param>
        /// <param name="publishSettingsFile">The publish settings file.</param>
        /// <param name="password">The password.</param>
        /// <param name="allowUntrusted">Deploy even if destination certificate is untrusted</param>
        /// <param name="doNotDelete">todo: describe doNotDelete parameter on DeployContentToOneSite</param>
        /// <param name="traceLevel">todo: describe traceLevel parameter on DeployContentToOneSite</param>
        /// <param name="whatIf">todo: describe whatIf parameter on DeployContentToOneSite</param>
        /// <param name="targetPath">todo: describe targetPath parameter on DeployContentToOneSite</param>
        /// <param name="useChecksum">todo: describe useChecksum parameter on DeployContentToOneSite</param>
        /// <param name="appOfflineEnabled">todo: describe appOfflineEnabled parameter on DeployContentToOneSite</param>
        /// <param name="logAction">todo: describe logAction parameter on DeployContentToOneSite</param>
        /// <param name="appDataSkipDirectiveEnabled">todo: describe appDataSkipDirectiveEnabled parameter on DeployContentToOneSite</param>
        /// <returns>DeploymentChangeSummary.</returns>
        private async Task<DeploymentChangeSummary> DeployContentToOneSiteAsync2(
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

            var sourceBaseOptions = new DeploymentBaseOptions();

            string destinationPath = SetBaseOptions(publishSettingsFile,
                out DeploymentBaseOptions destBaseOptions,
                allowUntrusted);

            destBaseOptions.TraceLevel = traceLevel;
            destBaseOptions.Trace += DestBaseOptions_Trace;

            if (appDataSkipDirectiveEnabled)
            {
                destBaseOptions.SkipDirectives.Add(
                    new DeploymentSkipDirective("AppData", "objectName=\"dirpath\",absolutePath=App_Data"));
            }

            if (applicationInsightsProfiler2SkipDirectiveEnabled)
            {
                destBaseOptions.SkipDirectives.Add(
                    new DeploymentSkipDirective("WebJobs",
                        "objectName=\"dirpath\",absolutePath=App_Data\\\\jobs\\\\continuous"));
                destBaseOptions.SkipDirectives.Add(
                    new DeploymentSkipDirective("ApplicationInsightsProfiler2",
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
                    throw new Exception("A source zip file can't be used with a physical target path");
                }

                sourceProvider = DeploymentWellKnownProvider.Package;
            }

            var syncOptions = new DeploymentSyncOptions
            {
                DoNotDelete = doNotDelete,
                WhatIf = whatIf,
                UseChecksum = useChecksum
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

                if (added)
                {
                    logAction?.Invoke($"Added deployment rule '{ruleName}'");
                }
                else
                {
                    logAction?.Invoke($"Could not add deployment rule '{ruleName}'");
                }
            }

            if (!doNotDelete)
            {
                if (targetProvider == DeploymentWellKnownProvider.DirPath && Directory.Exists(destinationPath)
                                                                          && string.IsNullOrWhiteSpace(
                                                                              publishSettingsFile))
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
            }

            using (DeploymentObject deploymentObject =
                DeploymentManager.CreateObject(sourceProvider, sourcePath, sourceBaseOptions))
            {
                DeploymentChangeSummary deployContentToOneSite;

                FileInfo appOfflineFile = null;

                if (targetProvider == DeploymentWellKnownProvider.DirPath
                    && Directory.Exists(destinationPath)
                    && string.IsNullOrWhiteSpace(publishSettingsFile))
                {
                    string appOfflineFilePath = Path.Combine(destinationPath, AppOfflineHtm);

                    appOfflineFile = new FileInfo(appOfflineFilePath);
                }

                if (appOfflineFile != null)
                {
                    await Task.Delay(appOfflineDelay).ConfigureAwait(false);
                }

                try
                {
                    if (appOfflineFile != null && !appOfflineFile.Exists)
                    {
                        using (FileStream fileStream = File.Create(appOfflineFile.FullName))
                        {
                        }
                    }

                    deployContentToOneSite =
                        deploymentObject.SyncTo(targetProvider, destinationPath, destBaseOptions, syncOptions);
                }
                finally
                {
                    if (appOfflineFile != null)
                    {
                        appOfflineFile.Refresh();

                        if (appOfflineFile.Exists)
                        {
                            logAction($"Deleting {AppOfflineHtm} file '{appOfflineFile.FullName}'");
                            appOfflineFile.Delete();
                            logAction($"Deleted {AppOfflineHtm} file '{appOfflineFile.FullName}'");
                        }
                    }
                }

                return deployContentToOneSite;
            }
        }

        private static bool AllowCertificateCallback(
            object sender,
            X509Certificate cert,
            X509Chain chain,
            SslPolicyErrors errors)
        {
            return true;
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

        private static string SetBaseOptions(
            string publishSettingsPath,
            out DeploymentBaseOptions deploymentBaseOptions,
            bool allowUntrusted)
        {
            if (File.Exists(publishSettingsPath))
            {
                var publishSettings = new PublishSettings(publishSettingsPath);

                deploymentBaseOptions = new DeploymentBaseOptions
                {
                    ComputerName = publishSettings.ComputerName,
                    UserName = publishSettings.Username,
                    Password = publishSettings.Password,
                    AuthenticationType = publishSettings.AuthenticationType
                };

                if (allowUntrusted || publishSettings.AllowUntrusted)
                {
                    ServicePointManager.ServerCertificateValidationCallback = AllowCertificateCallback;
                }

                return publishSettings.SiteName;
            }

            deploymentBaseOptions = new DeploymentBaseOptions();

            return string.Empty;
        }

        private void DestBaseOptions_Trace(object sender, DeploymentTraceEventArgs e)
        {
            DeploymentTraceEventHandler?.Invoke(sender, new CustomEventArgs(e.EventData, e.EventLevel, e.Message));
        }

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
            DeploymentChangeSummary deploymentChangeSummary = await DeployContentToOneSiteAsync2(sourcePath,
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
    }
}
