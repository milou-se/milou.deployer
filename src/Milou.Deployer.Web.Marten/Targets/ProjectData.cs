using Marten.Schema;

namespace Milou.Deployer.Web.Marten.Targets
{
    [MartenData]
    public record ProjectData
    {
        [ForeignKey(typeof(OrganizationData))]
        public string OrganizationId { get; set; }

        public string Id { get; set; }
    }
}