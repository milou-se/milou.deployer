namespace Milou.Deployer.Waws
{
    internal class DeploymentRule
    {
        public static readonly DeploymentRule DoNotDeleteRule = new(nameof(DoNotDeleteRule));

        public DeploymentRule(string name) => Name = name;

        public string Name { get; }
    }
}