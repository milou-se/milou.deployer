using System.Collections.Generic;

namespace Milou.Deployer.Web.Marten
{
    public class AgentPoolAssignmentData
    {
        public string Id { get; set; }

        public Dictionary<string, string> Agents { get; set; }
    }
}