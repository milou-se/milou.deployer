using System;
using System.IO;
using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.Tests.Integration
{
    internal sealed class TempDirectory : IDisposable
    {
        private TempDirectory(DirectoryInfo directory)
        {
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public DirectoryInfo Directory { get; private set; }

        public static TempDirectory CreateTempDirectory(string name = null)
        {
            return new TempDirectory(new DirectoryInfo(Path.Combine(Path.GetTempPath(),
                $"{name.WithDefault("MD-tmp")}-{DateTime.UtcNow.Ticks}")).EnsureExists());
        }

        public void Dispose()
        {
            if (Directory != null)
            {
                Directory.Refresh();

                if (Directory.Exists)
                {
                    Directory.Delete(true);
                }

                Directory = null;
            }
        }
    }
}
