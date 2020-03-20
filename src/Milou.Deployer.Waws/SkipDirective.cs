using JetBrains.Annotations;

namespace Milou.Deployer.Waws
{
    internal class SkipDirective
    {
        public SkipDirective(string name, string path, [CanBeNull] string description = null)
        {
            Name = name;
            Path = path;
            Description = description;
        }

        public string Name { get; }

        public string Path { get; }

        public string Description { get; }
    }
}