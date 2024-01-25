/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;

namespace Yarn.Unity.Editor
{
    // A simple class lets us use a delegate as an IEqualityComparer from
    // https://stackoverflow.com/a/4607559
    internal static class Compare
    {
        public static IEqualityComparer<T> By<T>(System.Func<T, T, bool> comparison)
        {
            return new DelegateComparer<T>(comparison);
        }

        private class DelegateComparer<T> : EqualityComparer<T>
        {
            private readonly System.Func<T, T, bool> comparison;

            public DelegateComparer(System.Func<T, T, bool> identitySelector)
            {
                this.comparison = identitySelector;
            }

            public override bool Equals(T x, T y)
            {
                return comparison(x, y);
            }

            public override int GetHashCode(T obj)
            {
                // Force LINQ to never refer to the hash of an object by
                // returning a constant for all values. This is inefficient
                // because LINQ can't use an internal comparator, but we're
                // already looking to use a delegate to do a more
                // fine-grained test anyway, so we want to ensure that it's
                // called.
                return 0;
            }
        }
    }
}
