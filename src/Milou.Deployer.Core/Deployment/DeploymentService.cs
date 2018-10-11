using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Arbor.Xdt;
using JetBrains.Annotations;
using Milou.Deployer.Core.ApplicationMetadata;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.IO;
using Milou.Deployer.Core.NuGet;
using Milou.Deployer.Core.Processes;
using Milou.Deployer.Core.XmlTransformation;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Core.Deployment
{
    public sealed class DeploymentService
    {
        public const string AppOfflineHtm = "App_Offline.htm";
        private readonly DirectoryCleaner _directoryCleaner;

        private readonly FileMatcher _fileMatcher;

        private readonly ILogger _logger;
        private readonly IWebDeployHelper _webDeployHelper;
        private readonly Func<IIISManager> _iisManager;

        private readonly PackageInstaller _packageInstaller;

        private readonly XmlTransformer _xmlTransformer;

        public DeploymentService(
            DeployerConfiguration deployerConfiguration,
            ILogger logger,
            [NotNull] IKeyValueConfiguration keyValueConfiguration,
            IWebDeployHelper webDeployHelper,
            Func<IIISManager> iisManager)
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
        }

        public DeployerConfiguration DeployerConfiguration { get; }

        public async Task<ExitCode> DeployAsync(
            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions, CancellationToken cancellationToken = default)
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

            var tempDirectoriesToClean = new List<DirectoryInfo>();
            var tempFilesToClean = new List<string>();

            try
            {
                _logger.Verbose("Executing deployment executions [{Length}]: {Executions}",
                    deploymentExecutionDefinitions.Length,
                    string.Join($"{Environment.NewLine}\t", deploymentExecutionDefinitions.Select(_ => $"'{_}'")));

                foreach (DeploymentExecutionDefinition deploymentExecutionDefinition in deploymentExecutionDefinitions)
                {
                    _logger.Information("Executing deployment execution: '{DeploymentExecutionDefinition}'",
                        deploymentExecutionDefinition);

                    const string TempPrefix = "MD-";

                    string uniqueSuffix = DateTime.Now.ToString("MMddHHmmssfff", CultureInfo.InvariantCulture);

                    string tempPath = Path.Combine(
                        Path.GetTempPath(),
                        $"{TempPrefix}{uniqueSuffix}{Guid.NewGuid().ToString().Substring(0, 6)}");

                    var directoryInfo = new DirectoryInfo(tempPath);
                    DirectoryInfo packageInstallTempDirectoryInfo = directoryInfo;

                    MayBe<InstalledPackage> installedMainPackage =
                        await _packageInstaller.InstallPackageAsync(deploymentExecutionDefinition,
                            packageInstallTempDirectoryInfo,
                            false).ConfigureAwait(false);

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

                    directoryInfo.Refresh();

                    DirectoryInfo[] packagesDirectory = directoryInfo.GetDirectories();

                    DirectoryInfo packageDirectory =
                        packagesDirectory.Single(directory => directory.Name.Equals(installedPackage.PackageId, StringComparison.OrdinalIgnoreCase));

                    SemanticVersion version = GetSemanticVersionFromDefinition(deploymentExecutionDefinition,
                        packageDirectory,
                        installedPackage.Version);

                    _logger.Verbose("Package version is {Version}", version.ToNormalizedString());

                    var possibleXmlTransformations = new List<FileMatch>();
                    var replaceFiles = new List<FileMatch>();

                    var environmentPackageResult = new EnvironmentPackageResult(true);

                    if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.EnvironmentConfig))
                    {
                        environmentPackageResult = await AddEnvironmentPackageAsync(deploymentExecutionDefinition,
                            packageInstallTempDirectoryInfo,
                            possibleXmlTransformations,
                            replaceFiles,
                            tempDirectoriesToClean,
                            version,
                            cancellationToken).ConfigureAwait(false);

                        if (!environmentPackageResult.IsSuccess)
                        {
                            return ExitCode.Failure;
                        }
                    }
                    else
                    {
                        _logger.Debug("Definition has no environment configuration specified");
                    }

                    var contentDirectory =
                        new DirectoryInfo(Path.Combine(packageDirectory.FullName, "Content"));

                    if (!contentDirectory.Exists)
                    {
                        _logger.Error("Content directory '{FullName}' does not exist", contentDirectory.FullName);
                        return ExitCode.Failure;
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
                        _logger.Debug("Possible replacing files {V}",
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
                        try
                        {
                            _logger.Debug(
                                "Found web config transformation {Transformation} for deployment execution definition {Deployment}",
                                deploymentExecutionDefinition.WebConfigTransformFile,
                                deploymentExecutionDefinition);

                            var transformFile = new FileInfo(deploymentExecutionDefinition.WebConfigTransformFile);

                            if (transformFile.Exists)
                            {
                                string tempFileName = Path.GetTempFileName();

                                var webConfig = new FileInfo(Path.Combine(contentDirectory.FullName, "web.config"));

                                if (webConfig.Exists)
                                {
                                    using (var x = new XmlTransformableDocument())
                                    {
                                        x.PreserveWhitespace = true;
                                        x.Load(webConfig.FullName);

                                        using (var transform = new Arbor.Xdt.XmlTransformation(transformFile.FullName))
                                        {
                                            bool succeed = transform.Apply(x);

                                            if (succeed)
                                            {
                                                using (var fsDestFileStream =
                                                    new FileStream(tempFileName, FileMode.OpenOrCreate))
                                                {
                                                    x.Save(fsDestFileStream);
                                                }
                                            }
                                        }
                                    }
                                }

                                var tempFileInfo = new FileInfo(tempFileName);

                                if (tempFileInfo.Exists && tempFileInfo.Length > 0)
                                {
                                    _logger.Information(
                                        "Successfully transformed web.config with transformation {Transformation}",
                                        deploymentExecutionDefinition.WebConfigTransformFile);
                                    tempFileInfo.CopyTo(webConfig.FullName, overwrite: true);
                                }
                                else
                                {
                                    _logger.Warning(
                                        "Failed to transform web.config with transformation {Transformation}",
                                        deploymentExecutionDefinition.WebConfigTransformFile);
                                }

                                tempFileInfo.Delete();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Could not apply web.config transform with {Transform}", deploymentExecutionDefinition.WebConfigTransformFile);
                            throw;
                        }
                    }

                    string uniqueTargetTempSuffix = DateTime.Now.ToString("MMddHHmmssfff", CultureInfo.InvariantCulture);

                    string uniqueTargetTempPath = Path.Combine(
                        Path.GetTempPath(),
                        $"{TempPrefix}t{uniqueTargetTempSuffix}{Guid.NewGuid().ToString().Substring(0, 6)}");

                    var targetTempDirectoryInfo =
                        new DirectoryInfo(uniqueTargetTempPath);

                    if (!targetTempDirectoryInfo.Exists)
                    {
                        _logger.Debug("Creating temp target directory '{FullName}'",
                            packageInstallTempDirectoryInfo.FullName);
                        targetTempDirectoryInfo.Create();
                    }

                    string wwwrootPath = Path.Combine(contentDirectory.FullName, "wwwroot");

                    var wwwRootDirectory = new DirectoryInfo(wwwrootPath);

                    DirectoryInfo applicationMetadataTargetDirectory =
                        wwwRootDirectory.Exists ? wwwRootDirectory : contentDirectory;

                    ApplicationMetadataCreator.SetVersionFile(installedMainPackage.Value,
                        applicationMetadataTargetDirectory,
                        deploymentExecutionDefinition,
                        xmlTransformedFiles,
                        replacedFiles,
                        environmentPackageResult);

                    _logger.Verbose("Copying content files to '{FullName}'", targetTempDirectoryInfo.FullName);

                    bool appOfflineEnabled = deploymentExecutionDefinition.AppOfflineEnabled(DeployerConfiguration
                        .WebDeploy.Rules.AppOfflineRuleEnabled);

                    bool usePublishSettingsFile =
                        !string.IsNullOrWhiteSpace(deploymentExecutionDefinition.PublishSettingsFile);

                    var targetAppOffline = new FileInfo(Path.Combine(targetTempDirectoryInfo.FullName, AppOfflineHtm));

                    if (appOfflineEnabled
                        && string.IsNullOrWhiteSpace(deploymentExecutionDefinition.PublishSettingsFile))
                    {
                        string sourceAppOffline = Path.Combine(contentDirectory.FullName, AppOfflineHtm);

                        if (!File.Exists(sourceAppOffline))
                        {
                            if (!targetAppOffline.Exists)
                            {
                                using (File.Create(targetAppOffline.FullName))
                                {
                                }

                                _logger.Debug("Created offline file '{File}'", targetAppOffline.FullName);

                                if (DeployerConfiguration.DefaultWaitTimeAfterAppOffline > TimeSpan.Zero)
                                {
                                    await Task.Delay(DeployerConfiguration.DefaultWaitTimeAfterAppOffline).ConfigureAwait(false);
                                }

                                tempFilesToClean.Add(targetAppOffline.FullName);
                            }
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
                    tempDirectoriesToClean.Add(packageInstallTempDirectoryInfo);

                    bool hasPublishSettingsFile =
                        !string.IsNullOrWhiteSpace(deploymentExecutionDefinition.PublishSettingsFile)
                        && File.Exists(deploymentExecutionDefinition.PublishSettingsFile);

                    if (hasPublishSettingsFile)
                    {
                        _logger.Debug("The publish settings file '{PublishSettingsFile}' exist",
                            deploymentExecutionDefinition.PublishSettingsFile);
                    }
                    else
                    {
                        _logger.Debug("The deployment definition has no publish setting file");
                    }

                    bool doNotDeleteEnabled = deploymentExecutionDefinition.DoNotDeleteEnabled(DeployerConfiguration
                        .WebDeploy.Rules.DoNotDeleteRuleEnabled);

                    bool useChecksumEnabled = deploymentExecutionDefinition.UseChecksumEnabled(DeployerConfiguration
                        .WebDeploy.Rules.UseChecksumRuleEnabled);

                    bool appDataSkipDirectiveEnabled = deploymentExecutionDefinition.AppDataSkipDirectiveEnabled(
                        DeployerConfiguration
                            .WebDeploy.Rules.AppDataSkipDirectiveEnabled);

                    bool applicationInsightsProfiler2SkipDirectiveEnabled =
                        deploymentExecutionDefinition.ApplicationInsightsProfiler2SkipDirectiveEnabled(
                            DeployerConfiguration
                                .WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled);

                    bool whatIfEnabled = deploymentExecutionDefinition.WhatIfEnabled(false);

                    _logger.Debug("{RuleName}: {DoNotDeleteEnabled}",
                        nameof(DeployerConfiguration.WebDeploy.Rules.DoNotDeleteRuleEnabled),
                        doNotDeleteEnabled);
                    _logger.Debug("{RuleName}: {AppOfflineEnabled}",
                        nameof(DeployerConfiguration.WebDeploy.Rules.AppOfflineRuleEnabled),
                        appOfflineEnabled);
                    _logger.Debug("{RuleName}: {UseChecksumEnabled}",
                        nameof(DeployerConfiguration.WebDeploy.Rules.UseChecksumRuleEnabled),
                        useChecksumEnabled);
                    _logger.Debug("{RuleName}: {AppDataSkipDirectiveEnabled}",
                        nameof(DeployerConfiguration.WebDeploy.Rules.AppDataSkipDirectiveEnabled),
                        appDataSkipDirectiveEnabled);
                    _logger.Debug("{RuleName}: {ApplicationInsightsProfiler2SkipDirectiveEnabled}",
                        nameof(DeployerConfiguration.WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled),
                        applicationInsightsProfiler2SkipDirectiveEnabled);
                    _logger.Debug("{RuleName}: {WhatIfEnabled}",
                        nameof(DeploymentExecutionDefinitionExtensions.WhatIfEnabled),
                        whatIfEnabled);

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

                    bool hasIisSiteName = deploymentExecutionDefinition.IisSiteName.HasValue();
                    IDeploymentChangeSummary summary;

                    try
                    {
                        using (IIISManager manager = _iisManager())
                        {
                            if (hasIisSiteName)
                            {
                                bool stopped = manager.StopSiteIfApplicable(deploymentExecutionDefinition);

                                if (!stopped)
                                {
                                    _logger.Error(
                                        "Could not stop IIS site for deployment execution definition {DeploymentExecutionDefinition}",
                                        deploymentExecutionDefinition);
                                    return ExitCode.Failure;
                                }
                            }

                            summary = await _webDeployHelper.DeployContentToOneSiteAsync(
                                targetTempDirectoryInfo.FullName,
                                deploymentExecutionDefinition.PublishSettingsFile,
                                DeployerConfiguration.DefaultWaitTimeAfterAppOffline,
                                doNotDelete: doNotDeleteEnabled,
                                appOfflineEnabled: appOfflineEnabled,
                                useChecksum: useChecksumEnabled,
                                whatIf: whatIfEnabled,
                                traceLevel: TraceLevel.Verbose,
                                appDataSkipDirectiveEnabled: appDataSkipDirectiveEnabled,
                                applicationInsightsProfiler2SkipDirectiveEnabled:
                                applicationInsightsProfiler2SkipDirectiveEnabled,
                                logAction: message => _logger.Debug("{Message}", message),
                                targetPath: hasPublishSettingsFile
                                    ? string.Empty
                                    : deploymentExecutionDefinition.TargetDirectoryPath
                            ).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Could not handle start/stop for iis site {Site}", deploymentExecutionDefinition.IisSiteName);
                        throw;
                    }

                    _logger.Information("Summary: {Summary}", summary.ToDisplayValue());
                }
            }
            finally
            {
                _directoryCleaner.CleanFiles(tempFilesToClean);
                _directoryCleaner.CleanDirectories(tempDirectoriesToClean);
            }

            return ExitCode.Success;
        }

        private static SemanticVersion GetSemanticVersionFromDefinition(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DirectoryInfo packageDirectory,
            SemanticVersion fallback)
        {
            SemanticVersion version = deploymentExecutionDefinition.SemanticVersion.HasValue
                ? deploymentExecutionDefinition.SemanticVersion.Value
                : SemanticVersion.TryParse(
                    packageDirectory.Name.Replace(
                        deploymentExecutionDefinition.PackageId,
                        "").TrimStart('.'),
                    out SemanticVersion semanticVersion)
                    ? semanticVersion
                    : fallback;

            return version;
        }

        private static ImmutableArray<EnvironmentFile> GetEnvironmentFiles(
            DirectoryInfo configContentDirectory,
            DeploymentExecutionDefinition deploymentExecutionDefinition)
        {
            int patternLength = DeploymentConstants.EnvironmentPackagePattern.Split('.').Length;

            ImmutableArray<EnvironmentFile> files =
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
            SemanticVersion version, CancellationToken cancellationToken = default)
        {
            _logger.Debug("Fetching environment configuration {EnvironmentConfig}",
                deploymentExecutionDefinition.EnvironmentConfig);

            string usedEnvironmentPackage = "";

            SemanticVersion expectedVersion = version;
            string expectedPackageId =
                $"{deploymentExecutionDefinition.PackageId}.{DeploymentConstants.EnvironmentLiteral}.{deploymentExecutionDefinition.EnvironmentConfig}";

            var listCommands = new List<string>
            {
                "list",
                expectedPackageId,
                "-AllVersions"
            };

            if (!string.IsNullOrWhiteSpace(DeployerConfiguration.NuGetConfig)
                && File.Exists(DeployerConfiguration.NuGetConfig))
            {
                listCommands.Add("-ConfigFile");
                listCommands.Add(DeployerConfiguration.NuGetConfig);
            }

            if (deploymentExecutionDefinition.IsPreRelease)
            {
                if (!DeployerConfiguration.AllowPreReleaseEnabled && !deploymentExecutionDefinition.Force)
                {
                    throw new InvalidOperationException(
                        $"The deployer configuration is set to not allow pre-releases, environment variable '{ConfigurationKeys.AllowPreReleaseEnvironmentVariable}'");
                }

                listCommands.Add("-PreRelease");
            }

            var allFoundEnvironmentPackages = new List<string>();

            ExitCode nugetListPackagesExitCode;

            using (var processRunner = new ProcessRunner())
            {
                nugetListPackagesExitCode =
                    await
                        processRunner.ExecuteAsync(
                            DeployerConfiguration.NuGetExePath,
                            arguments: listCommands,
                            standardOutLog: (message, _) =>
                            {
                                _logger.Verbose("Found package '{Message}'", message);
                                allFoundEnvironmentPackages.Add(message);
                            },
                            toolAction: _logger.Verbose,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (!nugetListPackagesExitCode.IsSuccess)
            {
                _logger.Error(
                    "No main NuGet package was installed for deployment definition {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinition);

                return new EnvironmentPackageResult(false);
            }

            string expectedMatch = $"{expectedPackageId} {expectedVersion.ToNormalizedString()}";

            List<string> matchingFoundEnvironmentPackage =
                allFoundEnvironmentPackages.Where(
                    packageFullName =>
                        packageFullName.Equals(expectedMatch, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (matchingFoundEnvironmentPackage.Count > 1)
            {
                _logger.Error("Found multiple environment packages matching '{ExpectedMatch}', {V}",
                    expectedMatch,
                    string.Join(", ", matchingFoundEnvironmentPackage.Select(package => $"'{package}'")));
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
                        expectedVersion);

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
                            false).ConfigureAwait(false);

                if (!installedEnvironmentPackage.HasValue)
                {
                    _logger.Error(
                        "No environment NuGet package was installed for deployment definition {DeploymentDefinition}",
                        deploymentDefinition);

                    return new EnvironmentPackageResult(false);
                }

                usedEnvironmentPackage = matchingFoundEnvironmentPackage.Single();

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

            return new EnvironmentPackageResult(true, usedEnvironmentPackage);
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
                _logger.Debug("There was no wellknown action defined for file '{FullName}'", item.File.FullName);
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
    }
}
