﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Milou.Deployer.Web.Core.Deployment.Packages;
using Milou.Deployer.Web.Core.Integration.Nexus;
using Milou.Deployer.Web.Core.NuGet;
using Milou.Deployer.Web.Core.Settings;
using Newtonsoft.Json;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    [UsedImplicitly]
    public class NexusPackageWebHook : IPackageWebHook
    {
        private const string NexusSignatureHeader = "X-Nexus-Webhook-Signature";

        private readonly IApplicationSettingsStore _applicationSettingsStore;

        private readonly ILogger _logger;

        public NexusPackageWebHook(IApplicationSettingsStore applicationSettingsStore, ILogger logger)
        {
            _applicationSettingsStore = applicationSettingsStore;
            _logger = logger;
        }

        public async Task<PackageUpdatedEvent?> TryGetWebHookNotification(
            HttpRequest request,
            string content,
            CancellationToken cancellationToken)
        {
            if (request.ContentType.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (!request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug("Web hook request is not json");
                return null;
            }

            if (!request.Headers.TryGetValue(NexusSignatureHeader, out var signature))
            {
                _logger.Debug("Web hook request does not contain nexus signature header");
                return null;
            }

            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.Debug("Nexus web hook request does not contain a valid signature");
                return null;
            }

            byte[] jsonBytes = Encoding.UTF8.GetBytes(content);

            NexusConfig nexusConfig = await GetSignatureKeyAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(nexusConfig.HmacKey))
            {
                _logger.Warning("HMAC Key for {Config} is empty, cannot process Nexus web hook request",
                    nameof(NexusConfig));
                return null;
            }

            using HMACSHA1 hasher = GetSignatureKey(nexusConfig);
            byte[] computedHash = hasher.ComputeHash(jsonBytes);
            byte[] expectedBytes = ((string)signature).FromHexToByteArray();

            if (!computedHash.SequenceEqual(expectedBytes))
            {
                _logger.Error("Nexus web hook signature validation failed");
                return null;
            }

            NexusWebHookNotification webHookNotification =
                JsonConvert.DeserializeObject<NexusWebHookNotification>(content);

            if (string.IsNullOrWhiteSpace(webHookNotification?.Audit?.Attributes?.Name))
            {
                _logger.Debug("Nexus web hook notification does not contain audit attribute name");
                return null;
            }

            string[] split =
                webHookNotification.Audit.Attributes.Name.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (split.Length != 2)
            {
                _logger.Debug("Unexpected attribute name value '{Name}' in Nexus JSON {Json}",
                    webHookNotification.Audit.Attributes.Name, content);
                return null;
            }

            string name = split[0];
            string version = split[1];

            if (!SemanticVersion.TryParse(version, out SemanticVersion semanticVersion))
            {
                _logger.Debug("Could not parse semantic version from Nexus web hook notification, '{Version}'",
                    version);
                return null;
            }

            var packageVersion = new PackageVersion(name, semanticVersion);
            _logger.Information("Successfully received Nexus web hook notification for package {Package}",
                packageVersion);

            return new PackageUpdatedEvent(packageVersion, nexusConfig.NuGetSource, nexusConfig.NuGetConfig);
        }

        private async Task<NexusConfig> GetSignatureKeyAsync(CancellationToken cancellationToken)
        {
            ApplicationSettings applicationSettings =
                await _applicationSettingsStore.GetApplicationSettings(cancellationToken);

            NexusConfig nexusConfig = applicationSettings.NexusConfig;

            return nexusConfig;
        }

        private HMACSHA1 GetSignatureKey(NexusConfig nexusConfig)
        {
            byte[] key = Encoding.UTF8.GetBytes(nexusConfig.HmacKey ?? throw new InvalidOperationException(
                $"{nameof(nexusConfig.HmacKey)} is required"));

            return new HMACSHA1(key);
        }
    }
}