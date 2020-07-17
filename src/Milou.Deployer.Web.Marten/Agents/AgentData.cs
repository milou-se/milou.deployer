using Marten.Schema;

namespace Milou.Deployer.Web.Marten.Agents
{
    public class AgentData
    {
        [Identity]
        public string AgentId { get; set; }
        public string AccessToken { get; set; }
    }
}