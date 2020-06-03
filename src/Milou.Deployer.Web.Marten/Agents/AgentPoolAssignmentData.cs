using System.Collections.Generic;

namespace Milou.Deployer.Web.Marten.Agents
{
    public class AgentPoolAssignmentData
    {
        public string Id { get; set; }

        public Dictionary<string, string> Agents { get; set; }
    }
}