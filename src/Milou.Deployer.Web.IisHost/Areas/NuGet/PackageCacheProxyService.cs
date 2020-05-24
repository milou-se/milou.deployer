using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Deployment.Packages;
using Milou.Deployer.Web.Core.NuGet;
using Milou.Deployer.Web.Core.Settings;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.NuGet
{
    public class PackageCacheProxyService : IPackageService, INotificationHandler<PackageUpdatedEvent>
    {
        private const string AllPackagesCacheKey = PackagesCacheKeyBaseUrn + ":AnyConfig";

        private const string PackagesCacheKeyBaseUrn = "urn:milou:deployer:web:packages:";
        private readonly IApplicationSettingsStore _applicationSettingsStore;
        private readonly ILogger _logger;
        private readonly IDistributedCache _memoryCache;
        private readonly IPackageService _packageService;

        public PackageCacheProxyService(IPackageService packageService,
            ILogger logger,
            IApplicationSettingsStore applicationSettingsStore,
            IDistributedCache memoryCache)
        {
            _packageService = packageService;
            _logger = logger;
            _applicationSettingsStore = applicationSettingsStore;
            _memoryCache = memoryCache;
        }

        public Task Handle(PackageUpdatedEvent notification, CancellationToken cancellationToken) =>
            ClearCache(
                notification.PackageVersion.Key,
                notification.NugetConfig,
                notification.NugetSource);

        public async Task<IReadOnlyCollection<PackageVersion>> GetPackageVersionsAsync(
            string packageId,
            bool useCache = true,
            bool includePreReleased = false,
            string? nugetPackageSource = null,
            string? nugetConfigFile = null,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = GetCacheKey(nugetConfigFile, nugetPackageSource, packageId);

            _logger.Verbose("Using package cache key {Key}", cacheKey);

            if (useCache)
            {
                var packages = await _memoryCache.Get<PackageVersions>(cacheKey, _logger, cancellationToken);
                _logger.Debug(
                    "Returning packages from cache with key {Key} for package id {PackageId}",
                    cacheKey,
                    packageId);

                if (packages?.Versions?.Length > 0)
                {
                    return packages.Versions
                        .Select(version => new PackageVersion(packageId, SemanticVersion.Parse(version)))
                        .ToImmutableArray();
                }
            }

            var addedPackages = (await _packageService.GetPackageVersionsAsync(packageId, useCache,
                includePreReleased, nugetPackageSource, nugetConfigFile, cancellationToken)).ToArray();

            if (addedPackages.Length > 0)
            {
                ApplicationSettings settings =
                    await _applicationSettingsStore.GetApplicationSettings(CancellationToken.None);
                var cacheTime = settings.CacheTime;

                var versions = addedPackages
                    .Select(version => version.Version.ToNormalizedString())
                    .ToArray();

                var packageVersions = new PackageVersions {Versions = versions};

                await _memoryCache.Set(cacheKey, packageVersions, _logger, cancellationToken);

                _logger.Debug(
                    "Cached {Packages} packages with key {CacheKey} for {Duration} seconds",
                    addedPackages.Length,
                    cacheKey,
                    cacheTime.TotalSeconds.ToString("F0"));
            }
            else
            {
                _logger.Debug("Added no packages to in-memory cache for cache key {CacheKey}", cacheKey);
            }

            return addedPackages;
        }

        public async Task ClearCache(string packageId, string notificationNugetConfig, string notificationNugetSource)
        {
            string cacheKey = GetCacheKey(notificationNugetConfig, notificationNugetSource, packageId);

            await _memoryCache.RemoveAsync(cacheKey);
        }

        private string GetCacheKey(string? nugetConfigFile, string? nugetPackageSource, string packageId)
        {
            string cacheKey = AllPackagesCacheKey;

            if (!string.IsNullOrWhiteSpace(nugetConfigFile))
            {
                string configCachePart = $"{PackagesCacheKeyBaseUrn}:{NormalizeKey(nugetConfigFile)}";

                cacheKey = !string.IsNullOrWhiteSpace(nugetPackageSource)
                    ? $"{configCachePart}:{NormalizeKey(nugetPackageSource)}"
                    : configCachePart;
            }
            else if (!string.IsNullOrWhiteSpace(nugetPackageSource))
            {
                cacheKey = $"{PackagesCacheKeyBaseUrn}:{NormalizeKey(nugetPackageSource)}";
            }

            cacheKey += $":{packageId}";

            return cacheKey;
        }

        private string NormalizeKey(string key) =>
            key.Replace(":", "_", StringComparison.OrdinalIgnoreCase)
                .Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(
                    Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
                    "_",
                    StringComparison.OrdinalIgnoreCase);
    }
}