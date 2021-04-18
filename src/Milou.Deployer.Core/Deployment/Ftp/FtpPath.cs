using System;
using System.Linq;
using JetBrains.Annotations;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public sealed class FtpPath : IEquatable<FtpPath>
    {
        public const string RootPath = "/";

        public static readonly FtpPath Root = new(RootPath, FileSystemType.Directory);

        public FtpPath([NotNull] string path, FileSystemType type)
        {
            CheckFileSystemTypeValue(type);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(path));
            }

            Path = path.Equals(RootPath, StringComparison.OrdinalIgnoreCase)
                ? RootPath
                : $"/{path.TrimStart(trimChar: '/').Replace("//", "/", StringComparison.Ordinal)}";

            Type = type;
        }

        public bool IsAppDataDirectoryOrFile =>
            Path.Split(
                    new[] {'/'},
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(
                    path => path.Equals("App_Data", StringComparison.OrdinalIgnoreCase));

        public bool IsRoot => Path.Equals(RootPath, StringComparison.OrdinalIgnoreCase);

        public FtpPath? Parent
        {
            get
            {
                if (IsRoot)
                {
                    return default;
                }

                string[] parts = Path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

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

        public string Path { get; }

        public FileSystemType Type { get; }

        public FtpPath Append([NotNull] FtpPath path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return new FtpPath(Path.TrimEnd(trimChar: '/') + path.Path, path.Type);
        }

        public bool ContainsPath([NotNull] FtpPath excluded)
        {
            if (excluded is null)
            {
                throw new ArgumentNullException(nameof(excluded));
            }

            string[] otherSegments = excluded.Path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            string[] segments = Path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

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

        public bool Equals(FtpPath? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase) && Type == other.Type;
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || (obj is FtpPath other && Equals(other));

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Path) * 397) ^ (int)Type;
            }
        }

        public static bool operator ==(FtpPath left, FtpPath right) => Equals(left, right);

        public static bool operator !=(FtpPath left, FtpPath right) => !Equals(left, right);

        public override string ToString() => $"{nameof(Path)}: {Path}, {nameof(Type)}: {Type}";

        public static bool TryParse(string? value, FileSystemType fileSystemType, out FtpPath? ftpPath)
        {
            CheckFileSystemTypeValue(fileSystemType);

            if (string.IsNullOrWhiteSpace(value))
            {
                ftpPath = default;
                return false;
            }

            if (!value!.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                ftpPath = default;
                return false;
            }

            if (value.Contains("\\", StringComparison.OrdinalIgnoreCase))
            {
                ftpPath = default;
                return false;
            }

            ftpPath = new FtpPath(value, fileSystemType);
            return true;
        }

        private static void CheckFileSystemTypeValue(FileSystemType type)
        {
            if (!Enum.IsDefined(typeof(FileSystemType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Invalid parameter value {type}");
            }
        }
    }
}