using System.Collections.Generic;

namespace Milou.Deployer.Core.Extensions
{
    public static class ObjectExtensions
    {
        public static bool HasValue<T>(this T item) where T : class => item is {};
    }

    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }
    }
}
