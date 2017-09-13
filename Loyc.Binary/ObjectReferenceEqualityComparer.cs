using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Loyc.Syntax;

namespace Loyc.Binary
{
    /// <summary>
    /// A generic object comparer that only uses object's reference, 
    /// ignoring any <see cref="IEquatable{T}"/> or <see cref="object.Equals(object)"/> overrides.
    /// </summary>
    public sealed class ObjectReferenceEqualityComparer<T> : EqualityComparer<T>
        where T : class
    {
        // ObjectReferenceEqualityComparer is based on Yurik's answer to this StackOverflow
        // question:
        // https://stackoverflow.com/questions/1890058/iequalitycomparert-that-uses-referenceequals

        private static IEqualityComparer<T> defaultComparer;

        /// <summary>
        /// Gets the default object reference comparer.
        /// </summary>
        /// <returns>The default object reference comparer.</returns>
        public new static IEqualityComparer<T> Default
        {
            get { return defaultComparer ?? (defaultComparer = new ObjectReferenceEqualityComparer<T>()); }
        }

        /// <inheritdoc/>
        public override bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        /// <inheritdoc/>
        public override int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}

