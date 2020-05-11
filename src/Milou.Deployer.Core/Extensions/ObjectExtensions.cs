namespace Milou.Deployer.Core.Extensions
{
    public static class ObjectExtensions
    {
        public static bool HasValue<T>(this T item) where T : class => item is {};
    }
}
