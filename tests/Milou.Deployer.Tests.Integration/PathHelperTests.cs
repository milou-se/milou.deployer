using System.IO;
using Milou.Deployer.Core.Deployment.Ftp;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class PathHelperTests
    {
        [Fact]
        public void TestRelativePathForSubDirectory()
        {
            var fileInfo = new FileInfo(@"C:\temp\test\abc\123.txt");

            string relativePath = PathHelper.RelativePath(fileInfo, new DirectoryInfo(@"C:\temp\test\"));

            Assert.Equal("/abc/123.txt", relativePath);
        }

        [Fact]
        public void TestRelativePathRootFile()
        {
            var fileInfo = new FileInfo(@"C:\temp\test\123.txt");

            string relativePath = PathHelper.RelativePath(fileInfo, new DirectoryInfo(@"C:\temp\test\"));

            Assert.Equal("/123.txt", relativePath);
        }
    }
}