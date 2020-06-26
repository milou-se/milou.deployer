using System;
using System.IO;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Configuration;

namespace Milou.Deployer.Web.Tests.Integration
{
    public sealed class TestHttpPort : IConfigureEnvironment, IDisposable
    {
        public TestHttpPort(PortPoolRental portPoolRental, DirectoryInfo tempDir)
        {
            Port = portPoolRental;
            TempDir = tempDir;
        }

        public PortPoolRental Port { get; }
        public DirectoryInfo TempDir { get; }

        public void Configure(EnvironmentConfiguration environmentConfiguration)
        {
            environmentConfiguration.HttpPort = Port.Port;
            environmentConfiguration.HttpEnabled = true;
            environmentConfiguration.ApplicationBasePath = TempDir.FullName;
            environmentConfiguration.ContentBasePath = TempDir.FullName;
        }

        public void Dispose() => Port?.Dispose();
    }
}