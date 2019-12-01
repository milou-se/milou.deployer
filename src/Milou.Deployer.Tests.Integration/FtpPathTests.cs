using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;

using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class FtpPathTests
    {
        [Fact]
        public void IsRootShouldBeTrueWhenRoot()
        {
            var fileSystemItem = new FtpPath("/", FileSystemType.Directory);

            Assert.True(fileSystemItem.IsRoot);
        }

        [Fact]
        public void RootPathShouldBeSlash()
        {
            var fileSystemItem = new FtpPath("/", FileSystemType.Directory);

            Assert.Equal("/", fileSystemItem.Path);
        }

        [Fact]
        public void SubPathShouldStartWithSlash()
        {
            var fileSystemItem = new FtpPath("/testpath", FileSystemType.Directory);

            Assert.Equal("/testpath", fileSystemItem.Path);
        }

        [Fact]
        public void ParentShouldBeFoundForSubPath()
        {
            var fileSystemItem = new FtpPath("/testpath/testsub", FileSystemType.Directory).Parent;

            Assert.Equal("/testpath", fileSystemItem.Path);
        }

        [Fact]
        public void DoubleSlashSubPathShouldStartWithSlash()
        {
            var fileSystemItem = new FtpPath("//testpath", FileSystemType.Directory);

            Assert.Equal("/testpath", fileSystemItem.Path);
        }

        [Fact]
        public void ContainsPathWithPartOfNameShouldBeFalse()
        {
            var fileSystemItem = new FtpPath("/a/bfolder", FileSystemType.Directory);

            Assert.False(fileSystemItem.ContainsPath(new FtpPath("/a/b", FileSystemType.Directory)));
        }

        [Fact]
        public void ContainsPathWithLongerPathShouldBeFalse()
        {
            var fileSystemItem = new FtpPath("/a/b/c", FileSystemType.Directory);

            Assert.False(fileSystemItem.ContainsPath(new FtpPath("/a/b/c/d", FileSystemType.Directory)));
        }

        [Fact]
        public void ContainsPathWithShorterPathShouldBeTrue()
        {
            var fileSystemItem = new FtpPath("/a/b/c/d", FileSystemType.Directory);

            Assert.True(fileSystemItem.ContainsPath(new FtpPath("/a/b/c", FileSystemType.Directory)));
        }

        [Fact]
        public void ContainsPathForSamePathShouldBeTrue()
        {
            var fileSystemItem = new FtpPath("/a/b/c", FileSystemType.Directory);

            Assert.True(fileSystemItem.ContainsPath(new FtpPath("/a/b/c", FileSystemType.Directory)));
        }

        [Fact]
        public void ContainsPathForSamePathWithSlashShouldBeTrue()
        {
            var fileSystemItem = new FtpPath("/a/b/c", FileSystemType.Directory);

            Assert.True(fileSystemItem.ContainsPath(new FtpPath("/a/b/c/", FileSystemType.Directory)));
        }

        [Fact]
        public void AppendFileToFolderShouldHaveFullPath()
        {
            var fileSystemItem = new FtpPath("/a", FileSystemType.Directory).Append(new FtpPath("/b/c.txt", FileSystemType.File));

            Assert.Equal("/a/b/c.txt", fileSystemItem.Path);
        }
    }
}