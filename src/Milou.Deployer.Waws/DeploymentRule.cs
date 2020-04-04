namespace Milou.Deployer.Waws
{
    internal class DeploymentRule
    {
        public DeploymentRule(string name) => Name = name;

        public string Name { get;  }
    }
}