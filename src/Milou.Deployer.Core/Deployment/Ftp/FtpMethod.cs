using System;
using System.Net;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public sealed class FtpMethod : IEquatable<FtpMethod>
    {
        public static readonly FtpMethod ListDirectory = new FtpMethod(nameof(WebRequestMethods.Ftp.ListDirectory),
            WebRequestMethods.Ftp.ListDirectory);

        public static readonly FtpMethod ListDirectoryDetails = new FtpMethod(
            nameof(WebRequestMethods.Ftp.ListDirectoryDetails),
            WebRequestMethods.Ftp.ListDirectoryDetails);

        public static readonly FtpMethod DeleteFile =
            new FtpMethod(nameof(WebRequestMethods.Ftp.DeleteFile), WebRequestMethods.Ftp.DeleteFile);

        public static readonly FtpMethod RemoveDirectory = new FtpMethod(nameof(WebRequestMethods.Ftp.RemoveDirectory),
            WebRequestMethods.Ftp.RemoveDirectory);

        public static readonly FtpMethod UploadFile =
            new FtpMethod(nameof(WebRequestMethods.Ftp.UploadFile), WebRequestMethods.Ftp.UploadFile);

        public static readonly FtpMethod GetFileSize =
            new FtpMethod(nameof(WebRequestMethods.Ftp.GetFileSize), WebRequestMethods.Ftp.GetFileSize);

        public static readonly FtpMethod MakeDirectory =
            new FtpMethod(nameof(WebRequestMethods.Ftp.MakeDirectory), WebRequestMethods.Ftp.MakeDirectory);

        private FtpMethod(string name, string command)
        {
            Command = command;
            Name = name;
        }

        public string Name { get; }

        public string Command { get; }

        public bool Equals(FtpMethod other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Command, other.Command, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((FtpMethod)obj);
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Command);

        public static bool operator ==(FtpMethod left, FtpMethod right) => Equals(left, right);

        public static bool operator !=(FtpMethod left, FtpMethod right) => !Equals(left, right);

        public override string ToString() => $"{Name} [{Command}]";
    }
}