namespace Milou.Deployer.Core.Deployment
{
    public class EnvironmentPackageResult
    {
        public EnvironmentPackageResult(bool isSuccess) : this(isSuccess, "")
        {
        }

        public EnvironmentPackageResult(bool isSuccess, string package)
        {
            IsSuccess = isSuccess;
            Package = package;
        }

        public bool IsSuccess { get; }

        public string Package { get; }
    }
}