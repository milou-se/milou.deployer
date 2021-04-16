using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.Core.Extensions
{
    public static class ColorExtensions
    {
        public static string ToStatusColor(this int value)
        {
            if (value == 0)
            {
                return "success";
            }

            return "failed";
        }
        public static string ToStatusColor(this WorkTaskStatus value)
        {
            return value.Status.ToLowerInvariant();
        }
    }
}