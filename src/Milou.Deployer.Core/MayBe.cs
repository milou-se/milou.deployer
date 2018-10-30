using System;

namespace Milou.Deployer.Core
{
    public sealed class MayBe<T>
        where T : class
    {
        private static readonly Lazy<MayBe<T>> _Nothing = new Lazy<MayBe<T>>(() => new MayBe<T>(null));

        private readonly T _value;

        public MayBe(T value)
        {
            _value = value;
        }

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

        public static implicit operator MayBe<T>(T item)
        {
            return item == null ? Nothing : new MayBe<T>(item);
        }

        public static MayBe<T> Nothing => _Nothing.Value;

        public MayBe<T> ToMayBe(T item)
        {
            return item == null ? Nothing : new MayBe<T>(item);
        }
    }
}
