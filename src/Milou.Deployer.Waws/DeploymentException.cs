using System;

namespace Milou.Deployer.Waws
{
    public sealed class DeploymentException : Exception
    {
        public DeploymentException(string message) : base(message)
        {
        }
    }
}