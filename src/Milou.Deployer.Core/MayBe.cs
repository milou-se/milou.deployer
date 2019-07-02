using System;

namespace Milou.Deployer.Core
{
    public sealed class MayBe<T>
        where T : class
    {
        private static readonly Lazy<MayBe<T>> LazyNothing = new Lazy<MayBe<T>>(() => new MayBe<T>(null));

        private readonly T _value;

        public MayBe(T value) => _value = value;

        public bool HasValue => _value != null;

        public T Value
        {
            get
            {
                if (_value == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot get a value for the type {typeof(T).FullName} since the value is null, make sure to call {nameof(HasValue)} first");
                }

                return _value;
            }
        }

        public static implicit operator T(MayBe<T> mayBe)
        {
            if (mayBe is null)
            {
                throw new ArgumentNullException(nameof(mayBe));
            }

            return mayBe.Value;
        }

        public static implicit operator MayBe<T>(T item) => item is null ? Nothing : new MayBe<T>(item);

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static MayBe<T> Nothing => LazyNothing.Value;
#pragma warning restore CA1000 // Do not declare static members on generic types

        public MayBe<T> ToMayBe(T item) => item is null ? Nothing : new MayBe<T>(item);
    }
}
