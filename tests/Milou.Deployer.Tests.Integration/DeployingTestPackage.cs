using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.IO;
using Milou.Deployer.DeployerApp;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class DeployingTestPackage
    {
        public DeployingTestPackage(ITestOutputHelper output) => _output = output;

        public const string PackageId = "MyPackage";

        private readonly ITestOutputHelper _output;

        private TempFile CreateTestManifestFile(DirectoryInfo testTargetDirectory, string nugetConfig)
        {
            var tempFile = TempFile.CreateTempFile(extension: "manifest");

            string json = $@"{{
  ""definitions"": [
    {{
      ""ExcludedFilePatterns"": """",
      ""EnvironmentConfig"": null,
      ""PublishSettingsFile"": null,
      ""Force"": false,
      ""PackageId"": ""{PackageId}"",
      ""Parameters"": {{}},
      ""TargetDirectoryPath"": ""{testTargetDirectory.FullName.Replace("\\", "\\\\")}"",
      ""IsPreRelease"": false,
      ""SemanticVersion"": ""1.0.0"",
      ""RequireEnvironmentConfig"": false,
      ""IisSitename"": null,
      ""NuGetConfigFile"": ""{nugetConfig.Replace("\\", "\\\\")}"",
      ""NuGetPackageSource"": null
    }}
  ]
}}
";

            File.WriteAllText(tempFile.File.FullName, json, Encoding.UTF8);

            _output.WriteLine(json);

            return tempFile;
        }

        [Fact]
        public async Task RunAsync()
        {
            string oldTemp = Path.GetTempPath();

            using var tempDir = TempDirectory.CreateTempDirectory();

            try
            {
                Environment.SetEnvironmentVariable("TEMP", tempDir.Directory.FullName);

                string nugetConfig = Path.Combine(
                    VcsTestPathHelper.FindVcsRootPath(),
                    "tests",
                    "Milou.Deployer.Tests.Integration",
                    "Config",
                    "NuGet.Config");

                if (!File.Exists(nugetConfig))
                {
                    throw new InvalidOperationException($"The file '{nugetConfig}' does not exist");
                }

                for (int i = 1; i <= 3; i++)
                {
                    _output.WriteLine($"RUN {i}");
                    int exitCode;
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var testTargetDirectory = TempDirectory.CreateTempDirectory();
                    using TempFile tempFile = CreateTestManifestFile(testTargetDirectory.Directory, nugetConfig);
                    string json = await File.ReadAllTextAsync(tempFile.File.FullName, Encoding.UTF8, cancellationTokenSource.Token);

                    _output.WriteLine(json);

                    var deploymentExecutionDefinition = JsonConvert.DeserializeAnonymousType(json,
                        new {definitions = Array.Empty<DeploymentExecutionDefinition>()});

                    Assert.NotNull(deploymentExecutionDefinition);
                    Assert.NotNull(deploymentExecutionDefinition.definitions);

                    Assert.Single(deploymentExecutionDefinition.definitions);

                    string[] args = {tempFile.File.FullName, "-nuget-config=" + nugetConfig};

                    Logger logger = new LoggerConfiguration()
                        .WriteTo.TestSink(_output)
                        .MinimumLevel.Verbose()
                        .CreateLogger();

                    using (logger)
                    {
                        using (DeployerApp.DeployerApp deployerApp = await
                            AppBuilder.BuildAppAsync(args, logger, cancellationTokenSource.Token))
                        {
                            exitCode = await deployerApp.ExecuteAsync(args,
                                cancellationTokenSource.Token);
                        }

                        logger?.Dispose();
                    }

                    FileInfo indexHtml = testTargetDirectory.Directory.GetFiles("index.html").SingleOrDefault();
                    Assert.NotNull(indexHtml);

                    DirectoryInfo wwwrootDirectory =
                        testTargetDirectory.Directory.GetDirectories("wwwroot").SingleOrDefault();

                    Assert.NotNull(wwwrootDirectory);
                    FileInfo applicationMetadata =
                        wwwrootDirectory.GetFiles("applicationmetadata.json").SingleOrDefault();
                    Assert.NotNull(applicationMetadata);

                    string text = await File.ReadAllTextAsync(applicationMetadata.FullName, cancellationTokenSource.Token);

                    var metadata = JsonConvert.DeserializeAnonymousType(
                        text,
                        new {keys = new List<KeyValuePair<string, string>>()});

                    Assert.NotNull(metadata.keys.SingleOrDefault(key =>
                        key.Key.Equals("existingkey", StringComparison.OrdinalIgnoreCase)).Value);

                    Assert.Equal(0, exitCode);

#pragma warning disable S1215 // "GC.Collect" should not be called
                    GC.Collect(0, GCCollectionMode.Forced);
                    GC.Collect(1, GCCollectionMode.Forced);
                    GC.Collect(2, GCCollectionMode.Forced);
#pragma warning restore S1215 // "GC.Collect" should not be called
                }
            }
            finally
            {
                tempDir.Directory.Refresh();
                if (tempDir.Directory.Exists)
                {
                    FileInfo[] files = tempDir.Directory.GetFiles();
                    DirectoryInfo[] directories = tempDir.Directory.GetDirectories();

                    foreach (DirectoryInfo dir in directories)
                    {
                        dir.Delete(true);
                    }

                    foreach (FileInfo file in files)
                    {
                        file.Delete();
                    }

                    Environment.SetEnvironmentVariable("TEMP", oldTemp);

                    Assert.Empty(files);
                    Assert.Empty(directories);
                }
            }
        }
    }
}