/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;

#nullable enable

static class EnumerableExtensions
{
    public static IEnumerable<T> NonNull<T>(this IEnumerable<T?> collection, bool throwIfAnyNull = false) where T : class
    {
        foreach (var item in collection)
        {
            if (item != null)
            {
                yield return item;
            }
            else
            {
                if (throwIfAnyNull)
                {
                    throw new NullReferenceException("Collection contains a null item");
                }
            }
        }
    }
}
