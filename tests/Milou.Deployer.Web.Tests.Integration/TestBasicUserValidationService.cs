using System.Threading.Tasks;
using AspNetCore.Authentication.Basic;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestBasicUserValidationService : IBasicUserValidationService
    {
        public Task<bool> IsValidAsync(string username, string password) => Task.FromResult(string.Equals("test", username) && string.Equals("test", password));
    }
}