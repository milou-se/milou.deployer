using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Agent
{
    [JsonConverter(typeof(ValueObjectConverter))]
    public abstract class ValueObject<TSelf, TValue> : IEquatable<ValueObject<TSelf, TValue>> where TSelf : ValueObject<TSelf, TValue> where TValue : notnull
    {
        protected virtual void Validate([NotNull] TValue value)
        {
        }

        [System.Diagnostics.Contracts.Pure]
        protected virtual TValue Transform(TValue value) => value;

        private readonly IComparable<TValue>? _comparable;
        public TValue Value { get; }

        protected ValueObject(TValue value, IComparable<TValue>? comparable = null)
        {
            _comparable = comparable;
            Validate(value);
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            Value = Transform(value);
        }

        public bool Equals(ValueObject<TSelf, TValue>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object? obj) => Equals(obj is TSelf self ? self : null);

        public override int GetHashCode() => EqualityComparer<TValue>.Default.GetHashCode(Value);

        public static bool operator ==(ValueObject<TSelf, TValue>? left, ValueObject<TSelf, TValue>? right) => Equals(left, right);

        public static bool operator !=(ValueObject<TSelf, TValue>? left, ValueObject<TSelf, TValue>? right) => !Equals(left, right);
    }
}