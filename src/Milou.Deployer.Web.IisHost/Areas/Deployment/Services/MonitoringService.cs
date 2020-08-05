﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Schema.Json;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Application.Metadata;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.Packages;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.IisHost.Areas.NuGet;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Services
{
    [UsedImplicitly]
    public class MonitoringService : INotificationHandler<DeploymentMetadataLog>,
        INotificationHandler<UpdateDeploymentTargetResult>
    {
        private readonly IApplicationSettingsStore _applicationSettingsStore;

        private readonly ICustomMemoryCache _customMemoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly NuGetListConfiguration _nuGetListConfiguration;
        private readonly IPackageService _packageService;
        private readonly TimeoutHelper _timeoutHelper;

        public MonitoringService(
            [NotNull] ILogger logger,
            [NotNull] IHttpClientFactory httpClientFactory,
            [NotNull] IPackageService packageService,
            TimeoutHelper timeoutHelper,
            NuGetListConfiguration nuGetListConfiguration,
            IApplicationSettingsStore applicationSettingsStore,
            ICustomMemoryCache customMemoryCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
            _timeoutHelper = timeoutHelper;
            _nuGetListConfiguration = nuGetListConfiguration;
            _applicationSettingsStore = applicationSettingsStore;
            _customMemoryCache = customMemoryCache;
        }

        public Task Handle(DeploymentMetadataLog notification, CancellationToken cancellationToken)
        {
            InvalidateCache(notification.DeploymentTask.DeploymentTargetId);

            return Task.CompletedTask;
        }

        public Task Handle(UpdateDeploymentTargetResult notification, CancellationToken cancellationToken)
        {
            InvalidateCache(notification.TargetId);

            return Task.CompletedTask;
        }

        public async Task<AppVersion?> GetAppMetadataAsync(
            [NotNull] DeploymentTarget target,
            CancellationToken cancellationToken)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            string cacheKey = GetCacheKey(target.Id);

            if (_customMemoryCache.TryGetValue(cacheKey, out AppVersion? appMetadata))
            {
                _logger.Verbose("App metadata fetched from cache {CacheKey}", cacheKey);
                return appMetadata;
            }

            ApplicationSettings applicationSettings =
                await _applicationSettingsStore.GetApplicationSettings(cancellationToken);

            var targetMetadataTimeout = target.MetadataTimeout ?? applicationSettings.ApplicationSettingsCacheTimeout;

            using (CancellationTokenSource cancellationTokenSource = _timeoutHelper.CreateCancellationTokenSource(
                targetMetadataTimeout))
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    cancellationTokenSource.Token.Register(() =>
                    {
                        _logger.Verbose(
                            "{Method} for {Target}, cancellation token invoked out after {Seconds} seconds",
                            nameof(GetAppMetadataAsync),
                            target,
                            targetMetadataTimeout.TotalSeconds.ToString("F1"));
                    });
                }

                using var linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
                Task<(HttpResponseMessage?, string)> metadataTask =
                    GetApplicationMetadataTask(target, linkedTokenSource.Token);

                Task<IReadOnlyCollection<PackageVersion>> allowedPackagesTask =
                    GetAllowedPackagesAsync(target, linkedTokenSource.Token);

                await Task.WhenAll(metadataTask, allowedPackagesTask);

                (HttpResponseMessage? response, string message) = await metadataTask;

                IReadOnlyCollection<PackageVersion> packages = await allowedPackagesTask;

                if (response is null)
                {
                    return new AppVersion(
                        target,
                        message ?? $"Could not get application metadata from target {target.Url}, no response",
                        packages);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new AppVersion(
                        target,
                        message ??
                        $"Could not get application metadata from target {target.Url}, status code not successful {response.StatusCode}",
                        packages);
                }

                appMetadata =
                    await GetAppVersionAsync(response, target, packages, linkedTokenSource.Token);

                response?.Dispose();
            }

            if (appMetadata.SemanticVersion is {})
            {
                _customMemoryCache.SetValue(cacheKey, appMetadata, applicationSettings.MetadataCacheTimeout);
            }


            return appMetadata;
        }

        private string GetCacheKey(string targetId) => $"{nameof(AppVersion)}:{targetId}";

        public async Task<IReadOnlyCollection<AppVersion>> GetAppMetadataAsync(
            IReadOnlyCollection<DeploymentTarget> targets,
            CancellationToken cancellationToken)
        {
            var appVersions = new List<AppVersion>();

            ApplicationSettings applicationSettings =
                await _applicationSettingsStore.GetApplicationSettings(cancellationToken);

            using (CancellationTokenSource cancellationTokenSource =
                _timeoutHelper.CreateCancellationTokenSource(applicationSettings.DefaultMetadataRequestTimeout))
            {
                using var linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
                var tasks = new Dictionary<string, Task<(HttpResponseMessage?, string)>>();

                foreach (DeploymentTarget deploymentTarget in targets)
                {
                    if (!deploymentTarget.Enabled)
                    {
                        appVersions.Add(new AppVersion(
                            deploymentTarget,
                            "Disabled",
                            ImmutableArray<PackageVersion>.Empty));
                    }
                    else if (deploymentTarget.Url is { })
                    {
                        Task<(HttpResponseMessage?, string)> getApplicationMetadataTask =
                            GetApplicationMetadataTask(deploymentTarget, linkedTokenSource.Token);

                        tasks.Add(deploymentTarget.Id, getApplicationMetadataTask);
                    }
                    else
                    {
                        appVersions.Add(new AppVersion(
                            deploymentTarget,
                            "Missing URL",
                            ImmutableArray<PackageVersion>.Empty));
                    }
                }

                await Task.WhenAll(tasks.Values);

                foreach (var pair in tasks)
                {
                    DeploymentTarget target = targets.Single(deploymentTarget => deploymentTarget.Id == pair.Key);

                    IReadOnlyCollection<PackageVersion> allowedPackages =
                        await GetAllowedPackagesAsync(target, linkedTokenSource.Token);

                    try
                    {
                        (var response, string message) = await pair.Value;

                        AppVersion appVersion;
                        using (response)
                        {
                            if (response is {} && response.IsSuccessStatusCode)
                            {
                                appVersion = await GetAppVersionAsync(
                                    response,
                                    target,
                                    allowedPackages,
                                    linkedTokenSource.Token);
                            }
                            else
                            {
                                if (message.HasValue())
                                {
                                    IReadOnlyCollection<PackageVersion> packages =
                                        await GetAllowedPackagesAsync(target, linkedTokenSource.Token);
                                    appVersion = new AppVersion(target, message, packages);

                                    _logger.Error(
                                        "Could not get metadata for target {Name} ({Url}), http status code {StatusCode}",
                                        target.Name,
                                        target.Url,
                                        response?.StatusCode.ToString() ?? Constants.NotAvailable);
                                }
                                else
                                {
                                    IReadOnlyCollection<PackageVersion> packages =
                                        await GetAllowedPackagesAsync(target, linkedTokenSource.Token);
                                    appVersion = new AppVersion(target, "Unknown error", packages);

                                    _logger.Error(
                                        "Could not get metadata for target {Name} ({Url}), http status code {StatusCode}",
                                        target.Name,
                                        target.Url,
                                        response?.StatusCode.ToString() ?? Constants.NotAvailable);
                                }
                            }
                        }

                        appVersions.Add(appVersion);
                    }
                    catch (JsonReaderException ex)
                    {
                        appVersions.Add(new AppVersion(target, ex.Message, ImmutableArray<PackageVersion>.Empty));

                        _logger.Error(
                            ex,
                            "Could not get metadata for target {Name} ({Url})",
                            target.Name,
                            target.Url);
                    }
                }
            }

            return appVersions;
        }

        private static async Task<AppVersion> GetAppVersionAsync(
            HttpResponseMessage response,
            DeploymentTarget target,
            IReadOnlyCollection<PackageVersion> filtered,
            CancellationToken cancellationToken)
        {
            if (response.Content.Headers.ContentType?.MediaType.Equals(
                "application/json",
                StringComparison.OrdinalIgnoreCase) != true)
            {
                return new AppVersion(target, "Response not JSON", filtered);
            }

            string json = await response.Content.ReadAsStringAsync();

            if (cancellationToken.IsCancellationRequested)
            {
                return new AppVersion(target, "Timeout", filtered);
            }

            ConfigurationItems configuration =
                JsonConfigurationSerializer.Deserialize(json);

            var nameValueCollection = new NameValueCollection();

            if (!configuration.Keys.IsDefaultOrEmpty)
            {
                foreach (KeyValue configurationItem in configuration.Keys)
                {
                    nameValueCollection.Add(configurationItem.Key, configurationItem.Value);
                }
            }

            var appVersion = new AppVersion(target, new InMemoryKeyValueConfiguration(nameValueCollection), filtered);

            return appVersion;
        }

        private async Task<(HttpResponseMessage?, string)> GetWrappedResponseAsync(
            DeploymentTarget deploymentTarget,
            Uri applicationMetadataUri,
            CancellationToken cancellationToken)
        {
            _logger.Debug("Making metadata request to {RequestUri} for target {Target}", applicationMetadataUri, deploymentTarget.Id);

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                HttpClient client = _httpClientFactory.CreateClient(applicationMetadataUri.Host);
                (HttpResponseMessage, string Empty) wrappedResponseAsync = (
                    await client.GetAsync(applicationMetadataUri, cancellationToken), string.Empty);
                stopwatch.Stop();
                _logger.Debug("Metadata call to {Url} took {Elapsed} milliseconds for target {Target}", applicationMetadataUri,
                    stopwatch.ElapsedMilliseconds, deploymentTarget.Id);

                return wrappedResponseAsync;
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error(
                    ex,
                    "Could not get application metadata for {ApplicationMetadataUri} target {Target}",
                    applicationMetadataUri,
                    deploymentTarget.Id);
                return ((HttpResponseMessage?)null, "Timeout");
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(
                    ex,
                    "Could not get application metadata for {ApplicationMetadataUri} target {Target}",
                    applicationMetadataUri,
                    deploymentTarget.Id);
                return ((HttpResponseMessage?)null, ex.Message);
            }
        }

        private async Task<IReadOnlyCollection<PackageVersion>> GetAllowedPackagesAsync(
            DeploymentTarget target,
            CancellationToken cancellationToken)
        {
            CancellationTokenSource? cancellationTokenSource;
            CancellationTokenSource? linkedCancellationTokenSource = null;
            if (target.NuGet.PackageListTimeout.HasValue)
            {
                cancellationTokenSource = _timeoutHelper.CreateCancellationTokenSource(target.NuGet.PackageListTimeout.Value);
                cancellationToken = cancellationTokenSource.Token;
            }
            else
            {
                cancellationTokenSource =
                    _timeoutHelper.CreateCancellationTokenSource(TimeSpan.FromSeconds(_nuGetListConfiguration.ListTimeOutInSeconds));

                linkedCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);
                cancellationToken = linkedCancellationTokenSource.Token;
            }

            try
            {
                IReadOnlyCollection<PackageVersion> allPackageVersions =
                    await _packageService.GetPackageVersionsAsync(
                        target.PackageId,
                        nugetConfigFile: target.NuGet.NuGetConfigFile,
                        nugetPackageSource: target.NuGet.NuGetPackageSource,
                        includePreReleased: target.AllowExplicitExplicitPreRelease == true || target.AllowPreRelease,
                        cancellationToken: cancellationToken);

                IReadOnlyCollection<PackageVersion> allTargetPackageVersions = allPackageVersions.Where(
                        packageVersion =>
                            target.PackageId.Equals(
                                packageVersion.PackageId,
                                StringComparison.OrdinalIgnoreCase))
                    .SafeToReadOnlyCollection();

                IReadOnlyCollection<PackageVersion> preReleaseFiltered = allTargetPackageVersions;

                if (!target.AllowPreRelease)
                {
                    preReleaseFiltered =
                        allTargetPackageVersions.Where(package => !package.Version.IsPrerelease)
                            .SafeToReadOnlyCollection();
                }

                IReadOnlyCollection<PackageVersion> filtered =
                    preReleaseFiltered.OrderByDescending(packageVersion => packageVersion.Version)
                        .SafeToReadOnlyCollection();

                return filtered;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not get allowed packages for target {Target}", target.Id);
                return ImmutableArray<PackageVersion>.Empty;
            }
            finally
            {
                cancellationTokenSource.Dispose();
                linkedCancellationTokenSource?.Dispose();
            }
        }

        private Task<(HttpResponseMessage?, string)> GetApplicationMetadataTask(
            DeploymentTarget deploymentTarget,
            CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(deploymentTarget.Url!) {Path = "applicationmetadata.json"};

            Uri applicationMetadataUri = uriBuilder.Uri;

            Task<(HttpResponseMessage?, string)> getApplicationMetadataTask = GetWrappedResponseAsync(
                deploymentTarget,
                applicationMetadataUri,
                cancellationToken);
            return getApplicationMetadataTask;
        }

        private void InvalidateCache(string targetId)
        {
            string cacheKey = GetCacheKey(targetId);

            _logger.Debug("Invalidating monitored app version for target {TargetId}", targetId);

            _customMemoryCache.Invalidate(cacheKey);
        }
    }
}