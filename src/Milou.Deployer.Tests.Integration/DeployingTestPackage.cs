using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.ConsoleClient;
using Milou.Deployer.Core;
using Milou.Deployer.Core.Deployment;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class DeployingTestPackage
    {
        public DeployingTestPackage(ITestOutputHelper output) => _output = output;

        public const string PackageId = "MilouDeployerWebTest";

        private readonly ITestOutputHelper _output;

        private TempFile CreateTestManifestFile(DirectoryInfo testTargetDirectory)
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
      ""Version"": ""1.2.0"",
      ""RequireEnvironmentConfig"": false,
      ""IisSitename"": null,
      ""NuGetConfigFile"": null,
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

            using (var tempDir = TempDirectory.CreateTempDirectory())
            {
                try
                {
                    Environment.SetEnvironmentVariable("TEMP", tempDir.Directory.FullName);

                    for (int i = 1; i <= 3; i++)
                    {
                        _output.WriteLine($"RUN {i}");
                        int exitCode;
                        using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                        {
                            using (var testTargetDirectory = TempDirectory.CreateTempDirectory())
                            {
                                using (var tempFile = CreateTestManifestFile(testTargetDirectory.Directory))
                                {
                                    string json = File.ReadAllText(tempFile.File.FullName, Encoding.UTF8);

                                    _output.WriteLine(json);

                                    var deploymentExecutionDefinition = JsonConvert.DeserializeAnonymousType(json,
                                        new { definitions = Array.Empty<DeploymentExecutionDefinition>() });

                                    Assert.NotNull(deploymentExecutionDefinition);
                                    Assert.NotNull(deploymentExecutionDefinition.definitions);

                                    Assert.Single(deploymentExecutionDefinition.definitions);

                                    string[] args = { tempFile.File.FullName };

                                    var logger = new LoggerConfiguration()
                                        .WriteTo.TestSink(_output)
                                        .MinimumLevel.Verbose()
                                        .CreateLogger();

                                    using (logger)
                                    {
                                        using (var deployerApp = await
                                            AppBuilder.BuildAppAsync(args, logger, cancellationTokenSource.Token))
                                        {
                                            exitCode = await deployerApp.ExecuteAsync(args,
                                                cancellationTokenSource.Token);
                                        }

                                        logger?.Dispose();
                                    }
                                }

                                var indexHtml = testTargetDirectory.Directory.GetFiles("index.html").SingleOrDefault();

                                Assert.NotNull(indexHtml);
                            }
                        }

                        if (exitCode != 0)
                        {
                            // break;
                        }

                        Assert.Equal(0, exitCode);

                        GC.Collect(0, GCCollectionMode.Forced);
                        GC.Collect(1, GCCollectionMode.Forced);
                        GC.Collect(2, GCCollectionMode.Forced);
                    }
                }
                finally
                {
                    tempDir.Directory.Refresh();
                    if (tempDir.Directory.Exists)
                    {
                        var files = tempDir.Directory.GetFiles();
                        var directories = tempDir.Directory.GetDirectories();

                        foreach (var dir in directories)
                        {
                            dir.Delete(true);
                        }

                        foreach (var file in files)
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
}