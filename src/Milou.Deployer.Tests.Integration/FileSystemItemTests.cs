using Milou.Deployer.Core.Deployment;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class FileSystemItemTests
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
    }
}