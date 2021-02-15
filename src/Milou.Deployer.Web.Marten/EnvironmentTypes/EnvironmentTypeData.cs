using Milou.Deployer.Web.Core.Deployment;

namespace Milou.Deployer.Web.Marten.EnvironmentTypes
{
    [MartenData]
    public record EnvironmentTypeData
    {
        public static readonly EnvironmentTypeData Empty = new() {Id = ""};

        public string PreReleaseBehavior { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public static EnvironmentTypeData MapToData(EnvironmentType environmentType) =>
            new()
            {
                Id = environmentType.Id.Trim(),
                PreReleaseBehavior = environmentType.PreReleaseBehavior.Name.Trim(),
                Name = environmentType.Name
            };

        public static EnvironmentType MapFromData(EnvironmentTypeData data) => new EnvironmentType(data.Id, data.Name,
            Core.Deployment.PreReleaseBehavior.Parse(data.PreReleaseBehavior));
    }
}