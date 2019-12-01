@ECHO OFF
SET Arbor.Builld.Build.Bootstrapper.AllowPrerelease=true
SET Arbor.Builld.Tools.External.MSpec.Enabled=true
SET Arbor.Builld.NuGet.Package.Artifacts.Suffix=
SET Arbor.Builld.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Builld.NuGetPackageVersion=
SET Arbor.Builld.Vcs.Branch.Name.Version.OverrideEnabled=true
SET Arbor.Builld.Vcs.Branch.Name=%GITHUB_REF%
SET Arbor.Builld.Build.VariableOverrideEnabled=true
SET Arbor.Builld.Artifacts.CleanupBeforeBuildEnabled=true
SET Arbor.Builld.Build.NetAssembly.Configuration=
SET Arbor.Builld.Tools.External.LibZ.Enabled=true
SET Arbor.Builld.MSBuild.NuGetRestore.Enabled=true

SET Arbor.Builld.NuGet.ReinstallArborPackageEnabled=true
SET Arbor.Builld.NuGet.VersionUpdateEnabled=false
SET Arbor.Builld.Artifacts.PdbArtifacts.Enabled=true
SET Arbor.Builld.NuGet.Package.CreateNuGetWebPackages.Enabled=true

SET Arbor.Builld.Build.NetAssembly.MetadataEnabled=true
SET Arbor.Builld.Build.NetAssembly.Description=Milou Deployer
SET Arbor.Builld.Build.NetAssembly.Company=Milou Communication AB
SET Arbor.Builld.Build.NetAssembly.Copyright=(C) Milou Communication AB 2015-2018
SET Arbor.Builld.Build.NetAssembly.Trademark=Milou Deployer
SET Arbor.Builld.Build.NetAssembly.Product=Milou Deployer
SET Arbor.Builld.ShowAvailableVariablesEnabled=false
SET Arbor.Builld.ShowDefinedVariablesEnabled=false
SET Arbor.Builld.Tools.External.MSBuild.Verbosity=minimal
SET Arbor.Builld.NuGet.Package.AllowManifestReWriteEnabled=false

SET Arbor.Builld.NuGet.Package.ExcludesCommaSeparated=Arbor.Builld.Bootstrapper.nuspec

CALL dotnet arbor-build

REM Restore variables to default

SET Arbor.Builld.Build.Bootstrapper.AllowPrerelease=
SET Arbor.Builld.Tools.External.MSpec.Enabled=
SET Arbor.Builld.NuGet.Package.Artifacts.Suffix=
SET Arbor.Builld.NuGet.Package.Artifacts.BuildNumber.Enabled=
SET Arbor.Builld.Log.Level=
SET Arbor.Builld.NuGetPackageVersion=
SET Arbor.Builld.Vcs.Branch.Name.Version.OverrideEnabled=
SET Arbor.Builld.VariableOverrideEnabled=
SET Arbor.Builld.Artifacts.CleanupBeforeBuildEnabled=
SET Arbor.Builld.Build.NetAssembly.Configuration=

EXIT /B %ERRORLEVEL%
