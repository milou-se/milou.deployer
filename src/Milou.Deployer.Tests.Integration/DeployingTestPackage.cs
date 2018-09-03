using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.ConsoleClient;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class DeployingTestPackage
    {
        public DeployingTestPackage(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        private TempFile CreateTestManifestFile(DirectoryInfo testTargetDirectory)
        {
            TempFile tempFile = TempFile.CreateTempFile(extension: "manifest");

            string json = $@"{{
  ""definitions"": [
    {{
      ""ExcludedFilePatterns"": """",
      ""EnvironmentConfig"": null,
      ""PublishSettingsFile"": null,
      ""Force"": false,
      ""PackageId"": ""MilouDeployerWebTest"",
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
            for (int i = 1; i <= 3; i++)
            {
                _output.WriteLine($"RUN {i}");
                int exitCode;
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    using (TempDirectory testTargetDirectory = TempDirectory.CreateTempDirectory())
                    {
                        using (TempFile tempFile = CreateTestManifestFile(testTargetDirectory.Directory))
                        {
                            string[] args = { tempFile.File.FullName };

                            Environment.SetEnvironmentVariable("urn:milou-deployer:tools:nuget:exe-path",
                                @"C:\Tools\NuGet\nuget.exe"); //TODO make nuget.exe path use default if not specified

                            Logger logger = new LoggerConfiguration().WriteTo.TestSink(_output).MinimumLevel
                                .Information()
                                .CreateLogger();

                            using (logger)
                            {
                                using (DeployerApp deployerApp =
                                    AppBuilder.BuildApp(args, logger, cancellationTokenSource.Token))
                                {
                                    exitCode = await deployerApp.ExecuteAsync(args, cancellationTokenSource.Token);
                                }

                                logger?.Dispose();
                            }
                        }
                    }
                }

                if (exitCode != 0)
                {
                    break;
                }

                Assert.Equal(0, exitCode);

                GC.Collect(0, GCCollectionMode.Forced);
                GC.Collect(1, GCCollectionMode.Forced);
                GC.Collect(2, GCCollectionMode.Forced);
            }
        }
    }
}
