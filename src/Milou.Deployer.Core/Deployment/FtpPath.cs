using System;
using System.Linq;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment
{
    public class FtpPath
    {
        public const string RootPath = "/";

        public static readonly FtpPath Root = new FtpPath(RootPath, FileSystemType.Directory);

        public FtpPath([NotNull] string path, FileSystemType type)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            if (path.Equals(RootPath, StringComparison.OrdinalIgnoreCase))
            {
                Path = RootPath;
            }
            else
            {
                Path = $"/{path.TrimStart('/').Replace("//", "/")}";
            }

            Type = type;
        }

        public bool IsAppDataDirectoryOrFile => Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(path => path.Equals("App_Data", StringComparison.OrdinalIgnoreCase));

        public string Path { get; }

        public FileSystemType Type { get; }

        public bool IsRoot => Path.Equals(RootPath, StringComparison.OrdinalIgnoreCase);

        public FtpPath Parent
        {
            get
            {
                if (IsRoot)
                {
                    throw new InvalidOperationException("Current path is root");
                }

                string[] parts = Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                {
                    return Root;
                }

                string[] segments = parts.Take(parts.Length - 1).ToArray();

                if (segments.Length == 0)
                {
                    return Root;
                }

                string newPath = string.Join("/", segments);

                return new FtpPath(newPath, FileSystemType.Directory);
            }
        }

        public override string ToString()
        {
            return $"{nameof(Path)}: {Path}, {nameof(Type)}: {Type}";
        }

        public bool ContainsPath([NotNull] FtpPath excluded)
        {
            if (excluded == null)
            {
                throw new ArgumentNullException(nameof(excluded));
            }


            string[] otherSegments = excluded.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string[] segments = Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (otherSegments.Length > segments.Length)
            {
                return false;
            }

            for (int i = 0; i < otherSegments.Length; i++)
            {
                if (!otherSegments[i].Equals(segments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
