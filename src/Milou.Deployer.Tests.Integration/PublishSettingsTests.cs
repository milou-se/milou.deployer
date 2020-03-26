using System.Threading.Tasks;
using Milou.Deployer.Waws;
using Xunit;
using static Milou.Deployer.Tests.Integration.TestFile;

namespace Milou.Deployer.Tests.Integration
{
    public class PublishSettingsTests
    {
        [Fact]
        public async Task LoadSampleFileShouldWork()
        {
            var publishSettings = await PublishSettings.Load(GetTestFile("sample.PublishSettings"));

            Assert.NotNull(publishSettings);
            Assert.Equal("$deploy-test", publishSettings.Username);
            Assert.Equal("deploy-test", publishSettings.SiteName);
            Assert.Equal("testPw", publishSettings.Password);
            Assert.Equal("deploy-test.scm.hosting.local:443", publishSettings.ComputerName);
        }
    }
}