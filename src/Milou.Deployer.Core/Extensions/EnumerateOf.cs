using System;
using System.Collections.Immutable;
using System.Linq;

namespace Milou.Deployer.Core.Extensions
{
    internal static class EnumerateOf<T> where T : class
    {
        private static readonly Lazy<ImmutableArray<T>> Lazy = new Lazy<ImmutableArray<T>>(GetAll);

        public static ImmutableArray<T> All => Lazy.Value;

        private static ImmutableArray<T> GetAll()
        {
            Type type = typeof(T);
            return type
                .GetFields()
                .Where(field =>
                    field.FieldType == type && field.IsInitOnly && field.IsStatic && field.IsPublic)
                .Select(t => (T) t.GetValue(null))
                .ToImmutableArray();
        }
    }
}