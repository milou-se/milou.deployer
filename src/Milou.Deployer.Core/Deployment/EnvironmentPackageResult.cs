using NuGet.Versioning;

namespace Milou.Deployer.Core.Deployment
{
    public class EnvironmentPackageResult
    {
        public EnvironmentPackageResult(bool isSuccess)
            : this(isSuccess, null)
        {
        }

        public EnvironmentPackageResult(bool isSuccess, SemanticVersion version)
        {
            IsSuccess = isSuccess;
            Version = version;
        }

        public bool IsSuccess { get; }

        public SemanticVersion Version { get; }
    }
}