# Milou Deployer

# This is a Windows only tool that can deploy NuGet web packages to either

* A local directory
* Any target remote supporting WebDeploy
* an FTP site

## Usage

The recommended way is to install Milou Deployer as a .NET Core global tool, see https://www.nuget.org/packages/Milou.Deployer.Bootstrapper.GlobalTool/

Then you run 

        milou-deploy maniftest.json

Where manifest.json follows this schema:

    {
        "definitions": [
            {
                "PublishType": "WebDeploy",
                "ExcludedFilePatterns": "*.user;*.cache",
                "EnvironmentConfig": null,
                "PublishSettingsFile": null,
                "Force": false,
                "PackageId": "MySamplePackageId",
                "Parameters": {
                    "urn:milou:deployer:tools:web-deploy:directives:application-insights-profiler-2-directive:enabled": [
                    "true"
                    ],
                    "urn:milou:deployer:tools:web-deploy:rules:do-not-delete:enabled": [
                    "false"
                    ],
                    "urn:milou:deployer:tools:web-deploy:rules:what-if:enabled": [
                    "false"
                    ],
                    "urn:milou:deployer:tools:web-deploy:directives:app-data-skip-directive:enabled": [
                    "false"
                    ],
                    "urn:milou:deployer:tools:web-deploy:rules:app-offline:enabled": [
                    "true"
                    ],
                    "urn:milou:deployer:tools:web-deploy:rules:use-checksum:enabled": [
                    "true"
                    ]
                },
                "SemanticVersion": "1.0.0",
                "TargetDirectoryPath": "C:\\Sites\\Sample",
                "IsPreRelease": false,
                "RequireEnvironmentConfig": false,
                "WebConfigTransformFile": null,
                "IisSiteName": null,
                "NuGetConfigFile": null,
                "NuGetPackageSource": null,
                "FtpPath": null
            }
        ]
    }

## Optional command line args:

* --help (shows help)
* --debug (enables debugging)
* --non-interactive (disables interactive user prompts, default is interactive if the user session is interactive)
* --plain-console (skips log level and other metadata for standard output)

# Local development

## Local setup

 * Ensure Docker Desktop for Windows is installed
 * run "docker volume create deploydata" once
 * Set Milou.Deployer.Web.IisHost as the startup project

Docker containers will be started by the application itself.