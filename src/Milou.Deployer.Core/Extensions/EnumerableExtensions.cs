using System;
using System.Collections.Generic;

namespace Milou.Deployer.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Tap<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (T item in enumerable)
            {
                action(item);
                yield return item;
            }
        }
    }
}