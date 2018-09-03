using System;
using System.IO;
using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.Tests.Integration
{
    internal sealed class TempFile : IDisposable
    {
        private TempFile(FileInfo file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public FileInfo File { get; private set; }

        public static TempFile CreateTempFile(string name = null, string extension = null)
        {
            return new TempFile(new FileInfo(Path.Combine(Path.GetTempPath(),
                $"{name.WithDefault("MD-tmp")}-{DateTime.UtcNow.Ticks}.{extension.WithDefault(".tmp")}")));
        }

        public void Dispose()
        {
            if (File != null)
            {
                File.Refresh();

                if (File.Exists)
                {
                    File.Delete();
                }

                File = null;
            }
        }
    }
}
