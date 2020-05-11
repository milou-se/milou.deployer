using System.Linq;
using System.Text;
using Arbor.App.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Unit
{
    public class HexTests
    {
        public HexTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;
        private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void CanReadAndWriteHex()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("Abc");

            string hexString = bytes.FromByteArrayToHexString();

            _testOutputHelper.WriteLine(hexString);

            byte[] convertedBytes = hexString.FromHexToByteArray();

            Assert.True(convertedBytes.SequenceEqual(bytes));
        }
    }
}