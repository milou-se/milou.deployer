using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Arbor.Processing;
using Arbor.Tooler;

using JetBrains.Annotations;
using Milou.Deployer.Core.ApplicationMetadata;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment.Configuration;
using Milou.Deployer.Core.Deployment.Ftp;
using Milou.Deployer.Core.Deployment.WebDeploy;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.IO;
using Milou.Deployer.Core.NuGet;
using Milou.Deployer.Core.XmlTransformation;
using Newtonsoft.Json;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Core.Deployment
{
    public sealed class DeploymentService
    {
        private readonly DirectoryCleaner _directoryCleaner;

        private readonly FileMatcher _fileMatcher;
        private readonly Func<DeploymentExecutionDefinition, IIisManager> _iisManager;

        private readonly ILogger _logger;

        private readonly PackageInstaller _packageInstaller;
        private readonly IWebDeployHelper _webDeployHelper;

        private readonly XmlTransformer _xmlTransformer;
        private readonly NuGetPackageInstaller _nugetPackageInstaller;

        public DeploymentService(
            DeployerConfiguration deployerConfiguration,
            ILogger logger,
            [NotNull] IKeyValueConfiguration keyValueConfiguration,
            IWebDeployHelper webDeployHelper,
            Func<DeploymentExecutionDefinition, IIisManager> iisManager,
            NuGetPackageInstaller nugetPackageInstaller)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (keyValueConfiguration == null)
            {
                throw new ArgumentNullException(nameof(keyValueConfiguration));
            }

            DeployerConfiguration =
                deployerConfiguration ?? throw new ArgumentNullException(nameof(deployerConfiguration));

            _directoryCleaner = new DirectoryCleaner(logger);

            _packageInstaller = new PackageInstaller(logger, deployerConfiguration, keyValueConfiguration);

            _fileMatcher = new FileMatcher(logger);

            _xmlTransformer = new XmlTransformer(logger, _fileMatcher);

            _logger = logger;
            _webDeployHelper = webDeployHelper;
            _iisManager = iisManager;
            _nugetPackageInstaller = nugetPackageInstaller;
        }

        public DeployerConfiguration DeployerConfiguration { get; }

        private ExitCode VerifyFiles(string fileListFile, DirectoryInfo contentDirectory)
        {
            var existingFiles = contentDirectory
                .GetFiles("*", SearchOption.AllDirectories)
                .Select(file => new
                {
                    File = file,
                    RelativePath = file.FullName.Substring(contentDirectory.FullName.Length).TrimStart('\\')
                })
                .ToArray();

            string[] contentFiles = existingFiles
                .Select(s => s.RelativePath)
                .ToArray();

            string json = File.ReadAllText(fileListFile, Encoding.UTF8);

            var fileList = JsonConvert.DeserializeAnonymousType(json,
                new { files = new[] { new { file = "", sha512Base64Encoded = "" } } });

            _logger.Debug("Verifying file list containing {FileCount} files", fileList.files.Length);

            string[] expectedFiles = fileList.files
                .Select(s => s.file.TrimStart('\\'))
                .ToArray();

            string[] extraFiles = contentFiles
                .Except(expectedFiles, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string[] missingFiles = expectedFiles
                .Except(contentFiles, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (extraFiles.Length > 0 || missingFiles.Length > 0)
            {
                if (extraFiles.Length > 0)
                {
                    _logger.Error("Found extra files {Files} on disk", extraFiles);
                }

                if (missingFiles.Length > 0)
                {
                    _logger.Error("Could not find defined files {Files} on disk defined in NuGet package",
                        missingFiles);
                }

                return ExitCode.Failure;
            }

            var dictionary = fileList.files.ToDictionary(s => s.file.TrimStart('\\'),
                s => s.sha512Base64Encoded,
                StringComparer.OrdinalIgnoreCase);

            using (var hashAlgorithm = SHA512.Create())
            {
                foreach (var item in existingFiles)
                {
                    string expectedChecksum = dictionary[item.RelativePath];

                    using var fs = new FileStream(item.File.FullName, FileMode.Open);

                    byte[] fileHash = hashAlgorithm.ComputeHash(fs);

                    string base64 = Convert.ToBase64String(fileHash);

                    if (!base64.Equals(expectedChecksum, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Checksum differs for file {item}");
                    }
                }
            }

            _logger.Debug("Successfully verified all content files in extracted package directory");

            return ExitCode.Success;
        }

        private static SemanticVersion GetSemanticVersionFromDefinition(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DirectoryInfo packageDirectory,
            SemanticVersion fallback)
        {
            SemanticVersion version = deploymentExecutionDefinition.SemanticVersion
                ?? ( SemanticVersion.TryParse(
                    packageDirectory.Name.Replace(
                        deploymentExecutionDefinition.PackageId,
                        "").TrimStart('.'),
                    out SemanticVersion semanticVersion)
                    ? semanticVersion
                    : fallback);

            return version;
        }

        private static ImmutableArray<EnvironmentFile> GetEnvironmentFiles(
            DirectoryInfo configContentDirectory,
            DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            int patternLength = DeploymentConstants.EnvironmentPackagePattern.Split('.').Length;

            var files =
                configContentDirectory.GetFiles("*.*", SearchOption.AllDirectories)
                    .Select(file => new EnvironmentFile(file, file.Name.Split('.')))
                    .Where(file => file.FileNameParts.Length == patternLength
                                   && file.FileNameParts.Skip(1)
                                       .First()
                                       .Equals(DeploymentConstants.EnvironmentLiteral,
                                           StringComparison.OrdinalIgnoreCase)
                                   && file.FileNameParts.Skip(2).First().Equals(
                                       deploymentExecutionDefinition.EnvironmentConfig,
                                       StringComparison.OrdinalIgnoreCase))
                    .ToImmutableArray();

            return files;
        }

        private async Task<EnvironmentPackageResult> AddEnvironmentPackageAsync(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DirectoryInfo tempDirectoryInfo,
            List<FileMatch> possibleXmlTransformations,
            List<FileMatch> replaceFiles,
            List<DirectoryInfo> tempDirectoriesToClean,
            SemanticVersion version,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Fetching environment configuration {EnvironmentConfig}",
                deploymentExecutionDefinition.EnvironmentConfig);

            SemanticVersion expectedVersion = version;
            string expectedPackageId =
                $"{deploymentExecutionDefinition.PackageId}.{DeploymentConstants.EnvironmentLiteral}.{deploymentExecutionDefinition.EnvironmentConfig}";

            ImmutableArray<SemanticVersion> allVersions = await _nugetPackageInstaller.GetAllVersionsAsync(
                                       new NuGetPackageId(expectedPackageId),
                                       allowPreRelease: expectedVersion.IsPrerelease,
                                       nuGetSource: deploymentExecutionDefinition.NuGetPackageSource,
                                       nugetConfig: deploymentExecutionDefinition.NuGetConfigFile,
                                       nugetExePath: deploymentExecutionDefinition.NuGetExePath,
                                       timeoutInSeconds: 35,
                                       adaptiveEnabled: deploymentExecutionDefinition.PackageListPrefixEnabled
            );

            var matchingFoundEnvironmentPackage = allVersions
                .Where(currentVersion => currentVersion == expectedVersion)
                .ToList();

            if (matchingFoundEnvironmentPackage.Count > 1)
            {
                _logger.Error("Found multiple environment packages matching '{ExpectedMatch}', {Found}",
                    expectedVersion.ToNormalizedString(),
                    string.Join(", ", matchingFoundEnvironmentPackage.Select(currentVersion => $"'{currentVersion.ToNormalizedString()}'")));
                return new EnvironmentPackageResult(false);
            }

            const string environmentConfigPrefix = "EF_";

            if (matchingFoundEnvironmentPackage.Any())
            {
                var tempInstallDirectory =
                    new DirectoryInfo(
                        Path.Combine(
                            tempDirectoryInfo.FullName,
                            $"{environmentConfigPrefix}tmp",
                            deploymentExecutionDefinition.EnvironmentConfig));

                var deploymentDefinition =
                    new DeploymentExecutionDefinition(
                        expectedPackageId,
                        tempInstallDirectory.FullName,
                        expectedVersion,
                        nugetExePath: deploymentExecutionDefinition.NuGetExePath,
                        nuGetPackageSource: deploymentExecutionDefinition.NuGetPackageSource,
                        nuGetConfigFile: deploymentExecutionDefinition.NuGetConfigFile);

                var tempOutputDirectory =
                    new DirectoryInfo(
                        Path.Combine(
                            tempDirectoryInfo.FullName,
                            $"{environmentConfigPrefix}out",
                            deploymentExecutionDefinition.EnvironmentConfig));

                MayBe<InstalledPackage> installedEnvironmentPackage =
                    await
                        _packageInstaller.InstallPackageAsync(
                            deploymentDefinition,
                            tempOutputDirectory,
                            false,
                            null,
                            cancellationToken).ConfigureAwait(false);

                if (!installedEnvironmentPackage.HasValue)
                {
                    _logger.Error(
                        "No environment NuGet package was installed for deployment definition {DeploymentDefinition}",
                        deploymentDefinition);

                    return new EnvironmentPackageResult(false);
                }

                var configContentDirectory =
                    new DirectoryInfo(
                        Path.Combine(tempOutputDirectory.FullName, expectedPackageId, "content"));

                if (!configContentDirectory.Exists)
                {
                    _logger.Debug("The content directory for the environment package does not exist");
                }
                else
                {
                    ImmutableArray<EnvironmentFile> environmentFiles = GetEnvironmentFiles(
                        configContentDirectory,
                        deploymentExecutionDefinition);

                    if (environmentFiles.Any())
                    {
                        foreach (EnvironmentFile item in environmentFiles)
                        {
                            FindMatches(item, possibleXmlTransformations, configContentDirectory, replaceFiles);
                        }
                    }
                    else
                    {
                        IEnumerable<string> fileNamesToConcat =
                            configContentDirectory.GetFiles().Select(file => $"'{file.Name}'");

                        string foundFiles = string.Join(", ", fileNamesToConcat);

                        _logger.Debug("Could not find any action files in package, all files {FoundFiles}",
                            foundFiles);
                    }
                }

                _logger.Verbose("Deleting transformation package temp directory '{TempOutputDirectory}'",
                    tempOutputDirectory);

                tempDirectoriesToClean.Add(tempOutputDirectory);
            }
            else
            {
                if (deploymentExecutionDefinition.RequireEnvironmentConfig)
                {
                    _logger.Error(
                        "Environment config was set to {EnvironmentConfig} but no package was found with id {ExpectedPackageId} and version {Version}, deployment definition require the environment config",
                        deploymentExecutionDefinition.EnvironmentConfig,
                        expectedPackageId,
                        expectedVersion.ToNormalizedString());

                    return new EnvironmentPackageResult(false);
                }

                _logger.Debug(
                    "Environment config was set to {EnvironmentConfig} but no package was found with id {ExpectedPackageId} and version {Version}",
                    deploymentExecutionDefinition.EnvironmentConfig,
                    expectedPackageId,
                    expectedVersion.ToNormalizedString());
            }

            var foundPackage = matchingFoundEnvironmentPackage.SingleOrDefault();

            return new EnvironmentPackageResult(true, foundPackage);
        }

        private ReplaceResult ReplaceFileIfMatchingFiles(FileMatch replacement, DirectoryInfo contentDirectory)
        {
            var replacedFiles = new List<string>();

            ImmutableArray<FileInfo> matchingFiles = _fileMatcher.Matches(replacement, contentDirectory);

            if (matchingFiles.Length > 1)
            {
                _logger.Error("Could not find a single matching file to transform, found multiple: {V}",
                    string.Join(", ", matchingFiles.Select(file => $"'{file.FullName}'")));
                return new ReplaceResult(false);
            }

            if (matchingFiles.Any())
            {
                FileInfo targetFileInfo = matchingFiles.Single();

                ExitCode replaceExitCode = ReplaceFile(
                    targetFileInfo,
                    replacement.ActionFile,
                    contentDirectory,
                    replacement.ActionFileRootDirectory);

                if (!replaceExitCode.IsSuccess)
                {
                    return new ReplaceResult(false);
                }

                replacedFiles.Add(targetFileInfo.Name);
            }
            else
            {
                _logger.Debug("Could not find any matching file for file replacement, looked for '{TargetName}'",
                    replacement.TargetName);
            }

            return new ReplaceResult(true, replacedFiles);
        }

        private void FindMatches(
            EnvironmentFile item,
            List<FileMatch> possibleXmlTransformations,
            DirectoryInfo configContentDirectory,
            List<FileMatch> replaceFiles)
        {
            string targetFile = $"{item.FileNameParts[0]}.{item.FileNameParts.Last()}";

            _logger.Debug("Found possible file target to transform '{TargetFile}'", targetFile);

            string action = item.FileNameParts.Skip(3).First();

            if (action.Equals("XdtTransform", StringComparison.OrdinalIgnoreCase))
            {
                possibleXmlTransformations.Add(
                    new FileMatch(targetFile, item.File, configContentDirectory));
            }
            else if (action.Equals("Replace", StringComparison.OrdinalIgnoreCase))
            {
                replaceFiles.Add(
                    new FileMatch(targetFile, item.File, configContentDirectory));
            }
            else
            {
                _logger.Debug("There was no well-known action defined for file '{FullName}'", item.File.FullName);
            }
        }

        private ExitCode ReplaceFile(
            FileInfo targetFileInfo,
            FileInfo replacement,
            DirectoryInfo targetRootDirectory,
            DirectoryInfo replacementRootDirectory)
        {
            string targetFile = targetFileInfo.FullName;

            _logger.Debug("Replacing file '{TargetFile}' with new file '{FullName}'", targetFile, replacement.FullName);

            File.Copy(replacement.FullName, targetFile, true);

            string targetRelativePath = targetFileInfo.GetRelativePath(targetRootDirectory);
            string replacementRelativePath = replacement.GetRelativePath(replacementRootDirectory);

            _logger.Debug("Replaced file '{TargetRelativePath}' with new file '{ReplacementRelativePath}'",
                targetRelativePath,
                replacementRelativePath);

            return ExitCode.Success;
        }

        public Task<ExitCode> DeployAsync(
            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions,
            SemanticVersion explicitVersion,
            CancellationToken cancellationToken = default)
        {
            if (!deploymentExecutionDefinitions.Any())
            {
                throw new ArgumentException("Argument is empty collection", nameof(deploymentExecutionDefinitions));
            }

            if (string.IsNullOrWhiteSpace(DeployerConfiguration.NuGetExePath))
            {
                throw new InvalidOperationException(
                    $"The NuGet exe path is not defined, try set key '{ConfigurationKeys.NuGetExePath}'");
            }

            if (!File.Exists(DeployerConfiguration.NuGetExePath))
            {
                _logger.Error("The nuget.exe at '{Path}' does not exist", DeployerConfiguration.NuGetExePath);
                throw new InvalidOperationException(
                    $"The nuget.exe at '{DeployerConfiguration.NuGetExePath}' does not exist");
            }

            return InternalDeployAsync(
                deploymentExecutionDefinitions,
                explicitVersion,
                cancellationToken);
        }

        private async Task<ExitCode> InternalDeployAsync(
            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions,
            SemanticVersion explicitVersion,
            CancellationToken cancellationToken = default)
        {
            var tempDirectoriesToClean = new List<DirectoryInfo>();
            var tempFilesToClean = new List<string>();

            try
            {
                _logger.Verbose("Executing deployment execution definitions [{Length}]: {Executions}",
                    deploymentExecutionDefinitions.Length,
                    string.Join($"{Environment.NewLine}\t", deploymentExecutionDefinitions.Select(_ => $"'{_}'")));

                foreach (DeploymentExecutionDefinition deploymentExecutionDefinition in deploymentExecutionDefinitions)
                {
                    string asJson = JsonConvert.SerializeObject(deploymentExecutionDefinition, Formatting.Indented);
                    _logger.Information("Executing deployment execution definition: '{DeploymentExecutionDefinition}'",
                        asJson);

                    const string TempPrefix = "MD-";

                    string uniqueSuffix = DateTime.Now.ToString("MMddHHmmssfff", CultureInfo.InvariantCulture);

                    string tempPath = Path.Combine(
                        Path.GetTempPath(),
                        $"{TempPrefix}{uniqueSuffix}{Guid.NewGuid().ToString().Substring(0, 6)}");

                    var tempWorkingDirectory = new DirectoryInfo(tempPath);
                    DirectoryInfo packageInstallTempDirectory = tempWorkingDirectory;

                    tempDirectoriesToClean.Add(packageInstallTempDirectory);

                    MayBe<InstalledPackage> installedMainPackage =
                        await _packageInstaller.InstallPackageAsync(
                            deploymentExecutionDefinition,
                            packageInstallTempDirectory,
                            false,
                            explicitVersion,
                            cancellationToken).ConfigureAwait(false);

                    if (!installedMainPackage.HasValue)
                    {
                        _logger.Error(
                            "Could not install package defined in deployment execution definition {DeploymentExecutionDefinition}",
                            deploymentExecutionDefinition);
                        return ExitCode.Failure;
                    }

                    InstalledPackage installedPackage = installedMainPackage.Value;

                    _logger.Information(
                        "Successfully installed NuGet package '{PackageId}' version '{Version}' to path '{NugetPackageFullPath}'",
                        installedPackage.PackageId,
                        installedPackage.Version.ToNormalizedString(),
                        installedPackage.NugetPackageFullPath);

                    tempWorkingDirectory.Refresh();

                    DirectoryInfo[] packagesDirectory = tempWorkingDirectory.GetDirectories();

                    DirectoryInfo packageDirectory =
                        packagesDirectory.Single(directory =>
                            directory.Name.Equals(installedPackage.PackageId, StringComparison.OrdinalIgnoreCase));

                    SemanticVersion version = explicitVersion ?? GetSemanticVersionFromDefinition(
                                                  deploymentExecutionDefinition,
                                                  packageDirectory,
                                                  installedPackage.Version);

                    _logger.Verbose("Package version is {Version}", version.ToNormalizedString());

                    var possibleXmlTransformations = new List<FileMatch>();
                    var replaceFiles = new List<FileMatch>();

                    var environmentPackageResult = new EnvironmentPackageResult(true);

                    var contentDirectory =
                        new DirectoryInfo(Path.Combine(packageDirectory.FullName, "Content"));

                    if (!contentDirectory.Exists)
                    {
                        _logger.Error("Content directory '{FullName}' does not exist", contentDirectory.FullName);
                        return ExitCode.Failure;
                    }

                    FileInfo contentFilesJson = packageDirectory.GetFiles("contentFiles.json").SingleOrDefault();

                    if (contentFilesJson?.Exists == true)
                    {
                        ExitCode exitCode = VerifyFiles(contentFilesJson.FullName, contentDirectory);

                        if (!exitCode.IsSuccess)
                        {
                            return exitCode;
                        }
                    }
                    else
                    {
                        _logger.Debug("No file contentFiles.json was found in package directory {PackageDirectory}",
                            packageDirectory.FullName);
                    }

                    if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.EnvironmentConfig))
                    {
                        _logger.Information("Fetching environment packages for package {Package} and environment {Environment}", deploymentExecutionDefinition.PackageId, deploymentExecutionDefinition.EnvironmentConfig);

                        environmentPackageResult = await AddEnvironmentPackageAsync(deploymentExecutionDefinition,
                            packageInstallTempDirectory,
                            possibleXmlTransformations,
                            replaceFiles,
                            tempDirectoriesToClean,
                            version,
                            cancellationToken).ConfigureAwait(false);

                        if (!environmentPackageResult.IsSuccess)
                        {
                            return ExitCode.Failure;
                        }

                        if (environmentPackageResult.Version != null)
                        {
                            _logger.Information("Installed environment package version {Version}",
                                environmentPackageResult.Version.ToNormalizedString());
                        }
                        else {
                            _logger.Information("No environment package was installed");
                        }
                    }
                    else
                    {
                        _logger.Debug("Definition has no environment configuration specified");
                    }

                    if (possibleXmlTransformations.Any())
                    {
                        _logger.Debug("Possible Xml transformation files {V}",
                            string.Join(", ",
                                possibleXmlTransformations.Select(fileMatch =>
                                    $"'{fileMatch.TargetName}' replaced by --> '{fileMatch.ActionFile.FullName}'")));
                    }

                    var xmlTransformedFiles = new List<string>();

                    foreach (FileMatch possibleXmlTransformation in possibleXmlTransformations)
                    {
                        TransformationResult result = _xmlTransformer.TransformMatch(possibleXmlTransformation,
                            contentDirectory);

                        if (!result.IsSuccess)
                        {
                            return ExitCode.Failure;
                        }

                        xmlTransformedFiles.AddRange(result.TransformedFiles);
                    }

                    if (replaceFiles.Any())
                    {
                        _logger.Debug("Possible replacing files {Files}",
                            string.Join(", ",
                                replaceFiles.Select(fileMatch =>
                                    $"'{fileMatch.TargetName}' replaced by --> '{fileMatch.ActionFile.FullName}'")));
                    }

                    var replacedFiles = new List<string>();

                    foreach (FileMatch replacement in replaceFiles)
                    {
                        ReplaceResult result = ReplaceFileIfMatchingFiles(replacement, contentDirectory);

                        if (!result.IsSuccess)
                        {
                            return ExitCode.Failure;
                        }

                        replacedFiles.AddRange(result.ReplacedFiles);
                    }

                    if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.WebConfigTransformFile))
                    {
                        DeploymentTransformation.Transform(deploymentExecutionDefinition, contentDirectory, _logger);
                    }

                    string uniqueTargetTempSuffix =
                        DateTime.Now.ToString("MMddHHmmssfff", CultureInfo.InvariantCulture);

                    string uniqueTargetTempPath = Path.Combine(
                        Path.GetTempPath(),
                        $"{TempPrefix}t{uniqueTargetTempSuffix}{Guid.NewGuid().ToString().Substring(0, 6)}");

                    var targetTempDirectoryInfo =
                        new DirectoryInfo(uniqueTargetTempPath);

                    if (!targetTempDirectoryInfo.Exists)
                    {
                        _logger.Debug("Creating temp target directory '{FullName}'",
                            packageInstallTempDirectory.FullName);
                        targetTempDirectoryInfo.Create();
                    }

                    string wwwrootPath = Path.Combine(contentDirectory.FullName, "wwwroot");

                    var wwwRootDirectory = new DirectoryInfo(wwwrootPath);

                    DirectoryInfo applicationMetadataTargetDirectory =
                        wwwRootDirectory.Exists ? wwwRootDirectory : contentDirectory;

                    string versionFile = ApplicationMetadataCreator.SetVersionFile(
                        installedMainPackage.Value,
                        applicationMetadataTargetDirectory,
                        deploymentExecutionDefinition,
                        xmlTransformedFiles,
                        replacedFiles,
                        environmentPackageResult,
                        _logger);

                    _logger.Information("Successfully wrote metadata file {Path}", versionFile);

                    _logger.Verbose("Copying content files to '{FullName}'", targetTempDirectoryInfo.FullName);

                    bool usePublishSettingsFile =
                        !string.IsNullOrWhiteSpace(deploymentExecutionDefinition.PublishSettingsFile);

                    var targetAppOffline = new FileInfo(Path.Combine(targetTempDirectoryInfo.FullName,
                        DeploymentConstants.AppOfflineHtm));

                    var ruleConfiguration = RuleConfiguration.Get(deploymentExecutionDefinition,
                        DeployerConfiguration,
                        _logger);

                    if (ruleConfiguration.AppOfflineEnabled && usePublishSettingsFile)
                    {
                        string sourceAppOffline =
                            Path.Combine(contentDirectory.FullName, DeploymentConstants.AppOfflineHtm);

                        if (!File.Exists(sourceAppOffline) && !targetAppOffline.Exists)
                        {
                            using var _ = File.Create(targetAppOffline.FullName);

                            _logger.Debug("Created offline file '{File}'", targetAppOffline.FullName);

                            if (DeployerConfiguration.DefaultWaitTimeAfterAppOffline > TimeSpan.Zero)
                            {
                                await Task.Delay(DeployerConfiguration.DefaultWaitTimeAfterAppOffline, cancellationToken)
                                    .ConfigureAwait(false);
                            }

                            tempFilesToClean.Add(targetAppOffline.FullName);
                        }
                    }

                    RecursiveIO.RecursiveCopy(contentDirectory,
                        targetTempDirectoryInfo,
                        _logger,
                        deploymentExecutionDefinition.ExcludedFilePatterns);

                    tempDirectoriesToClean.Add(targetTempDirectoryInfo);

                    _logger.Debug("Copied content files from '{ContentDirectory}' to '{FullName}'",
                        contentDirectory,
                        targetTempDirectoryInfo.FullName);
                    tempDirectoriesToClean.Add(packageInstallTempDirectory);

                    bool hasPublishSettingsFile =
                        !string.IsNullOrWhiteSpace(deploymentExecutionDefinition.PublishSettingsFile)
                        && File.Exists(deploymentExecutionDefinition.PublishSettingsFile);

                    if (hasPublishSettingsFile)
                    {
                        _logger.Debug("The publish settings file '{PublishSettingsFile}' exists",
                            deploymentExecutionDefinition.PublishSettingsFile);
                    }
                    else
                    {
                        _logger.Debug("The deployment definition has no publish setting file");
                    }

                    if (deploymentExecutionDefinition.PublishType == PublishType.WebDeploy)
                    {
                        _webDeployHelper.DeploymentTraceEventHandler += (sender, args) =>
                        {
                            if (string.IsNullOrWhiteSpace(args.Message))
                            {
                                return;
                            }

                            if (args.EventLevel == TraceLevel.Verbose)
                            {
                                _logger.Verbose("{Message}", args.Message);
                                return;
                            }

                            _logger.Information("{Message}", args.Message);
                        };
                    }

                    bool hasIisSiteName = deploymentExecutionDefinition.IisSiteName.HasValue();
                    IDeploymentChangeSummary summary;

                    try
                    {
                        using IIisManager manager = _iisManager(deploymentExecutionDefinition);

                        if (hasIisSiteName)
                        {
                            bool stopped = manager.StopSiteIfApplicable();

                            if (!stopped)
                            {
                                _logger.Error(
                                    "Could not stop IIS site for deployment execution definition {DeploymentExecutionDefinition}",
                                    deploymentExecutionDefinition);
                                return ExitCode.Failure;
                            }
                        }

                        try
                        {
                            if (deploymentExecutionDefinition.PublishType == PublishType.WebDeploy)
                            {
                                _logger.Information("Deploying {Target} with WebDeploy", deploymentExecutionDefinition.TargetDirectoryPath);
                                summary = await _webDeployHelper.DeployContentToOneSiteAsync(
                                              targetTempDirectoryInfo.FullName,
                                              deploymentExecutionDefinition.PublishSettingsFile,
                                              DeployerConfiguration.DefaultWaitTimeAfterAppOffline,
                                              doNotDelete: ruleConfiguration.DoNotDeleteEnabled,
                                              appOfflineEnabled: ruleConfiguration.AppOfflineEnabled,
                                              useChecksum: ruleConfiguration.UseChecksumEnabled,
                                              whatIf: ruleConfiguration.WhatIfEnabled,
                                              traceLevel: TraceLevel.Verbose,
                                              appDataSkipDirectiveEnabled: ruleConfiguration.AppDataSkipDirectiveEnabled,
                                              applicationInsightsProfiler2SkipDirectiveEnabled:
                                              ruleConfiguration.ApplicationInsightsProfiler2SkipDirectiveEnabled,
                                              logAction: message => _logger.Debug("{Message}", message),
                                              targetPath: hasPublishSettingsFile
                                                              ? string.Empty
                                                              : deploymentExecutionDefinition.TargetDirectoryPath
                                          ).ConfigureAwait(false);
                            }
                            else if (deploymentExecutionDefinition.PublishType.IsAnyFtpType)
                            {
                                var basePath = deploymentExecutionDefinition.FtpPath;

                                bool isSecure = deploymentExecutionDefinition.PublishType == PublishType.Ftps;

                                var ftpSettings = new FtpSettings(basePath, isSecure);

                                _logger.Information("Deploying {Target} with {PublishType}", deploymentExecutionDefinition.FtpPath?.Path, deploymentExecutionDefinition.PublishType);
                                string publishSettingsFile = deploymentExecutionDefinition.PublishSettingsFile;

                                if (string.IsNullOrWhiteSpace(publishSettingsFile))
                                {
                                    _logger.Error("Deployment target type is set to {Type} but no publish file is set", deploymentExecutionDefinition.PublishTypeValue);
                                    return ExitCode.Failure;
                                }

                                using FtpHandler ftpHandler = await FtpHandler.CreateWithPublishSettings(
                                    publishSettingsFile,
                                    ftpSettings,
                                    _logger);

                                _logger.Verbose("Created FTP handler, starting publish");

                                summary = await ftpHandler.PublishAsync(
                                              ruleConfiguration,
                                              targetTempDirectoryInfo,
                                              cancellationToken);
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"Publish type {deploymentExecutionDefinition.PublishType} is not supported");
                            }
                        }
                        catch (Exception ex) when (!ex.IsFatal())
                        {
                            _logger.Error(ex,
                                "Could not deploy site {DeploymentExecutionDefinition}",
                                deploymentExecutionDefinition);

                            return ExitCode.Failure;
                        }
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        _logger.Error(ex,
                            "Could not handle start/stop for iis site {Site}",
                            deploymentExecutionDefinition.IisSiteName);

                        return ExitCode.Failure;
                    }

                    _logger.Information("Summary: {Summary}", summary.ToDisplayValue());
                }
            }
            finally
            {
                string[] targetPaths = deploymentExecutionDefinitions
                    .Select(deploymentExecutionDefinition =>
                        deploymentExecutionDefinition.TargetDirectoryPath)
                    .Where(targetPath => !string.IsNullOrWhiteSpace(targetPath))
                    .Select(path => Path.Combine(path, DeploymentConstants.AppOfflineHtm))
                    .ToArray();

                tempFilesToClean.AddRange(targetPaths);

                await _directoryCleaner.CleanFilesAsync(tempFilesToClean);
                await _directoryCleaner.CleanDirectoriesAsync(tempDirectoriesToClean);
            }

            return ExitCode.Success;
        }
    }
}
