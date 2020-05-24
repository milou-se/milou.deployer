using System;
using System.IO;
using Arbor.App.Extensions.ExtensionMethods;


namespace Milou.Deployer.Tests.Integration
{
    internal sealed class TempDirectory : IDisposable
    {
        private TempDirectory(DirectoryInfo directory) =>
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));

        public DirectoryInfo Directory { get; private set; }

        public void Dispose()
        {
            if (Directory is { })
            {
                Directory?.Refresh();

                if (Directory?.Exists == true)
                {
                    try
                    {
                        Directory?.Delete(true);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // ignore
                    }
                }

                Directory = null!;
            }
        }

        public static TempDirectory CreateTempDirectory(string? name = null)
        {
            var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(),
                $"{name.WithDefault("MD-tmp")}-{DateTime.UtcNow.Ticks}"));

            return new TempDirectory(directory.EnsureExists());
        }
    }
}