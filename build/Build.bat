@ECHO OFF
SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.Build.Tools.External.MSpec.Enabled=true
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.NuGetPackageVersion=
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.Build.Vcs.Branch.Name=%GITHUB_REF%
SET Arbor.Build.Build.VariableOverrideEnabled=true
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.Build.Build.NetAssembly.Configuration=
SET Arbor.Build.Tools.External.LibZ.Enabled=true
SET Arbor.Build.MSBuild.NuGetRestore.Enabled=true

SET Arbor.Build.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.Build.NuGet.VersionUpdateEnabled=false
SET Arbor.Build.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.Build.NuGet.Package.CreateNuGetWebPackages.Enabled=true

SET Arbor.Build.Build.NetAssembly.MetadataEnabled=true
SET Arbor.Build.Build.NetAssembly.Description=Milou Deployer
SET Arbor.Build.Build.NetAssembly.Company=Milou Communication AB
SET Arbor.Build.Build.NetAssembly.Copyright=(C) Milou Communication AB 2015-2018
SET Arbor.Build.Build.NetAssembly.Trademark=Milou Deployer
SET Arbor.Build.Build.NetAssembly.Product=Milou Deployer
SET Arbor.Build.ShowAvailableVariablesEnabled=false
SET Arbor.Build.ShowDefinedVariablesEnabled=false
SET Arbor.Build.Tools.External.MSBuild.Verbosity=minimal
SET Arbor.Build.NuGet.Package.AllowManifestReWriteEnabled=false

SET Arbor.Build.NuGet.Package.ExcludesCommaSeparated=Arbor.Build.Bootstrapper.nuspec

CALL dotnet arbor-build

REM Restore variables to default

SET Arbor.Build.Build.Bootstrapper.AllowPrerelease=
SET Arbor.Build.Tools.External.MSpec.Enabled=
SET Arbor.Build.NuGet.Package.Artifacts.Suffix=
SET Arbor.Build.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Build.Log.Level=
SET Arbor.Build.NuGetPackageVersion=
SET Arbor.Build.Vcs.Branch.Name.Version.OverrideEnabled=
SET Arbor.Build.VariableOverrideEnabled=
SET Arbor.Build.Artifacts.CleanupBeforeBuildEnabled=
SET Arbor.Build.Build.NetAssembly.Configuration=

EXIT /B %ERRORLEVEL%
