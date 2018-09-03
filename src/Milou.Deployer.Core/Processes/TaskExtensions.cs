using System.Threading.Tasks;

namespace Milou.Deployer.Core.Processes
{
    internal static class TaskExtensions
    {
        public static bool CanBeAwaited(this Task task)
        {
            return task.IsCompleted || task.IsFaulted || task.IsCanceled;
        }

        public static bool CanBeAwaited<T>(this Task<T> task)
        {
            return task.IsCompleted || task.IsFaulted || task.IsCanceled;
        }
    }
}
