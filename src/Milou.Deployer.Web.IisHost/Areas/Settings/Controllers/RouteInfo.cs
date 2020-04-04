using System;

namespace Milou.Deployer.Web.IisHost.Areas.Settings.Controllers
{
    public class RouteInfo
    {
        public RouteInfo(string type, string name, string value)
        {
            Type = type;
            Name = name;
            Value = value;
        }

        public string Type { get; }

        public string Name { get; }

        public string Value { get; }

        public string RouteName => Name + "Name";

        public bool IsLinkable() => !Value.Contains("{", StringComparison.InvariantCulture) && !Name.Contains("Post", StringComparison.Ordinal);
    }
}
