using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Arbor.KVConfiguration.Schema.Json;
using JetBrains.Annotations;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.Core.ApplicationMetadata
{
    public static class ApplicationMetadataCreator
    {
        public static void SetVersionFile(
            [NotNull] InstalledPackage installedPackage,
            [NotNull] DirectoryInfo targetDirectoryInfo,
            [NotNull] DeploymentExecutionDefinition deploymentExecutionDefinition,
            [NotNull] IEnumerable<string> xmlTransformedFiles,
            [NotNull] IEnumerable<string> replacedFiles,
            [NotNull] EnvironmentPackageResult environmentPackageResult)
        {
            if (installedPackage == null)
            {
                throw new ArgumentNullException(nameof(installedPackage));
            }

            if (targetDirectoryInfo == null)
            {
                throw new ArgumentNullException(nameof(targetDirectoryInfo));
            }

            if (deploymentExecutionDefinition == null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            if (xmlTransformedFiles == null)
            {
                throw new ArgumentNullException(nameof(xmlTransformedFiles));
            }

            if (replacedFiles == null)
            {
                throw new ArgumentNullException(nameof(replacedFiles));
            }

            if (environmentPackageResult == null)
            {
                throw new ArgumentNullException(nameof(environmentPackageResult));
            }

            string applicationMetadataJsonFilePath = Path.Combine(targetDirectoryInfo.FullName,
                ConfigurationKeys.ApplicationMetadataFileName);

            var existingKeys = new List<KeyValue>();

            if (File.Exists(applicationMetadataJsonFilePath))
            {
                string json = File.ReadAllText(applicationMetadataJsonFilePath, Encoding.UTF8);

                ConfigurationItems configurationItems = JsonConfigurationSerializer.Deserialize(json);

                if (!configurationItems.Keys.IsDefaultOrEmpty)
                {
                    existingKeys.AddRange(configurationItems.Keys);
                }
            }

            var version = new KeyValue(ConfigurationKeys.SemVer2Normalized,
                installedPackage.Version.ToNormalizedString(),
                null);

            var packageId = new KeyValue(ConfigurationKeys.PackageId,
                installedPackage.PackageId,
                null);

            var deployStartTimeUtc = new KeyValue(
                ConfigurationKeys.DeployStartTimeUtc,
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                null);

            var deployedFromMachine = new KeyValue(
                ConfigurationKeys.DeployerDeployedFromMachine,
                Environment.MachineName,
                null);

            var deployerAssemblyVersion = new KeyValue(
                ConfigurationKeys.DeployerAssemblyVersion,
                GetAssemblyVersion(),
                null);

            var deployerAssemblyFileVersion = new KeyValue(
                ConfigurationKeys.DeployerAssemblyFileVersion,
                GetAssemblyFileVersion(),
                null);

            var environmentConfiguration = new KeyValue(
                ConfigurationKeys.DeployerEnvironmentConfiguration,
                deploymentExecutionDefinition.EnvironmentConfig,
                null);

            ImmutableArray<KeyValue> keys = new List<KeyValue>(existingKeys)
            {
                version,
                deployStartTimeUtc,
                deployerAssemblyVersion,
                deployerAssemblyFileVersion,
                packageId
            }.ToImmutableArray();

            if (!string.IsNullOrWhiteSpace(environmentPackageResult.Package))
            {
                keys.Add(environmentConfiguration);
            }

            string serialized = JsonConfigurationSerializer.Serialize(new ConfigurationItems("1.0", keys));

            File.WriteAllText(applicationMetadataJsonFilePath, serialized, Encoding.UTF8);
        }

        private static string GetAssemblyFileVersion()
        {
            Assembly currentAssembly = typeof(ApplicationMetadataCreator).Assembly;

            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(currentAssembly.Location);

                string fileVersion = fvi.FileVersion;

                return fileVersion;
            }
            catch (Exception ex) when(!ex.IsFatal())
            {
                try
                {
                    return currentAssembly.ImageRuntimeVersion;
                }
                catch (Exception innerEx) when(!innerEx.IsFatal())
                {
                    // ignored
                }

                return "N/A";
            }
        }

        private static string GetAssemblyVersion()
        {
            Assembly currentAssembly = typeof(ApplicationMetadataCreator).Assembly;

            AssemblyName assemblyName = currentAssembly.GetName();

            string assemblyVersion = assemblyName.Version.ToString();

            return assemblyVersion;
        }
    }
}
