using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Arbor.KVConfiguration.Schema.Json;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;

namespace Milou.Deployer.Core.ApplicationMetadata
{
    public static class ApplicationMetadataCreator
    {
        public static void SetVersionFile(
            InstalledPackage installedPackage,
            DirectoryInfo targetDirectoryInfo,
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            IEnumerable<string> xmlTransformedFiles,
            IEnumerable<string> replacedFiles,
            EnvironmentPackageResult environmentPackageResult)
        {
            string applicationMetadataJsonFilePath = Path.Combine(targetDirectoryInfo.FullName,
                ConfigurationKeys.ApplicationMetadataFileName);

            var existingKeys = new List<KeyValue>();

            var jsonConfigurationSerializer = new JsonConfigurationSerializer();

            if (File.Exists(applicationMetadataJsonFilePath))
            {
                string json = File.ReadAllText(applicationMetadataJsonFilePath, Encoding.UTF8);

                ConfigurationItems configurationItems = jsonConfigurationSerializer.Deserialize(json);
                existingKeys.AddRange(configurationItems.Keys);
            }

            var version = new KeyValue(ConfigurationKeys.SemVer2Normalized,
                installedPackage.Version.ToNormalizedString(),
                null);

            var packageId = new KeyValue(ConfigurationKeys.PackageId,
                installedPackage.PackageId,
                null);

            var deployStartTimeUtc = new KeyValue(
                ConfigurationKeys.DeployStartTimeUtc,
                DateTime.UtcNow.ToString("o"),
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

            string serialized = jsonConfigurationSerializer.Serialize(new ConfigurationItems("1.0", keys));

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
            catch (Exception)
            {
                try
                {
                    return currentAssembly.ImageRuntimeVersion;
                }
                catch (Exception)
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
