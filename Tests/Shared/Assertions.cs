/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Yarn.Unity.Tests
{

    public partial class ObjectAssertions<TSubject>
    {

        [AllowNull]
        public TSubject Subject { get; internal set; }

        protected static void NullCheck([NotNull] object? assertion, string? message)
        {
            if (assertion == null)
            {
                throw new AssertionException("Value is null", message);
            }
        }

        public void BeNull(string? message = null)
        {
            if (Subject != null)
            {
                throw new AssertionException($"Expected subject to be null, but it was non-null", message);
            }
        }

        public void NotBeNull(string? message = null)
        {
            if (Subject == null)
            {
                throw new AssertionException($"Expected subject to not be null, but it was null", message);
            }
        }

        public void BeEqualTo(TSubject? other, string? message = null)
        {
            var comparer = EqualityComparer<TSubject>.Default;

            if (Subject == null && other == null) {
                // They're both null, so they're equal
                return;
            }

            NullCheck(Subject, message);
            NullCheck(other, message);

            if (comparer.Equals(Subject, other) != true)
            {
                throw new AssertionException($"Expected subject to be equal to {other}, but it was {Subject}", message);
            }
        }

        public void NotBeEqualTo(TSubject? other, string? message = null)
        {
            var comparer = EqualityComparer<TSubject>.Default;

            if (Subject == null && other != null
            || Subject != null && other == null
            ) {
                // One of them is not null, so they're not equal
                return;
            }

            if ((Subject == null && other == null) || comparer.Equals(Subject, other!) == true)
            {
                throw new AssertionException($"Expected subject to not be equal to {other}, but it was {Subject}", message);
            }
        }

        public ObjectAssertions<TSubject> BeSameObjectAs(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(other, message);

            if (ReferenceEquals(Subject, other) == false)
            {
                throw new AssertionException($"Expected subject to be the same object as {other}, but it was {Subject}", message);
            }
            return this;
        }

        public ObjectAssertions<TSubject> NotBeSameObjectAs(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(other, message);

            if (ReferenceEquals(Subject, other) == true)
            {
                throw new AssertionException($"Expected subject to not be the same object as {other}, but it was {Subject}", message);
            }
            return this;
        }

        public ObjectAssertions<TSubject> BeOfType<T>(string? message = null)
        {
            NullCheck(Subject, message);

            if (typeof(T).IsAssignableFrom(Subject.GetType()) == false)
            {
                throw new AssertionException($"Expected subject to be {typeof(T)}, but it was {Subject.GetType()}", message);
            }

            return this;
        }

        public ObjectAssertions<TSubject> BeOfExactType<T>(string? message = null)
        {
            NullCheck(Subject, message);

            if (typeof(T) != Subject.GetType())
            {
                throw new AssertionException($"Expected subject to be exactly {typeof(T)}, but it was {Subject.GetType()}", message);
            }

            return this;
        }
    }

    public partial class BooleanAssertions : ObjectAssertions<bool>
    {

        public void BeTrue(string? message = null)
        {
            if (Subject != true)
            {
                throw new AssertionException($"Expected subject to be true, but it was false", message);
            }
        }

        public void BeFalse(string? message = null)
        {
            if (Subject != false)
            {
                throw new AssertionException($"Expected subject to be false, but it was true", message);
            }
        }

    }

    public class ComparableAssertions<TSubject> : ObjectAssertions<TSubject> where TSubject : IComparable<TSubject>
    {

        public void BeGreaterThan(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) != 1)
            {
                throw new AssertionException($"Expected {Subject} to be greater than {other}", message);
            }
        }

        public void BeLessThan(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) != -1)
            {
                throw new AssertionException($"Expected {Subject} to be greater than {other}", message);
            }
        }

        public void BeGreaterThanOrEqualTo(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) == -1)
            {
                throw new AssertionException($"Expected {Subject} to be greater than or equal to {other}", message);
            }
        }

        public void BeLessThanOrEqualTo(TSubject other, string? message = null)
        {

            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) == 1)
            {
                throw new AssertionException($"Expected {Subject} to be less than or equal to {other}", message);
            }
        }
    }

    public class NumericAssertions<TItem> : ComparableAssertions<TItem> where TItem : IComparable<TItem>
    {

    }

    public class EnumerableAssertions<TItem> : ObjectAssertions<IEnumerable<TItem>>
    {


        public EnumerableAssertions<TItem> BeEmpty(string? message = null)
        {
            NullCheck(Subject, message);

            if (Subject.Count() != 0)
            {
                throw new AssertionException($"Expected collection to be empty, but it had {Subject.Count()} items", message);
            }
            return this;
        }
        public EnumerableAssertions<TItem> NotBeEmpty(string? message = null)
        {
            NullCheck(Subject, message);

            if (Subject.Count() == 0)
            {
                throw new AssertionException($"Expected collection to not be empty", message);
            }
            return this;
        }


        public ObjectAssertions<TItem> Contain(Func<TItem, bool> predicate, string? message = null)
        {
            NullCheck(Subject, message);

            foreach (var item in Subject)
            {
                if (predicate(item) == true)
                {
                    return new ObjectAssertions<TItem> { Subject = item };
                }
            }

            throw new AssertionException($"Expected collection to contain item matching predicate", message);

        }

        public void Contain(TItem match, string? message = null)
        {
            var comparer = EqualityComparer<TItem>.Default;
            Contain((TItem item) => comparer.Equals(item, match), message);
        }



        public void ContainExactly(IEnumerable<TItem> expectation, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(expectation, message);

            foreach (var item in Subject)
            {
                if (expectation.Contains(item) == false)
                {
                    throw new AssertionException($"Expected collection to not contain {item}", message);
                }
            }
            foreach (var item in expectation)
            {
                if (Subject.Contains(item) == false)
                {
                    throw new AssertionException($"Expected collection to contain {item}", message);
                }
            }
        }


        public void ContainAllOf(IEnumerable<TItem> expectation, string? message = null)
        {

            NullCheck(Subject, message);

            foreach (var item in expectation)
            {
                if (Subject.Contains(item) == false)
                {
                    throw new AssertionException($"Expected collection to contain {item}", message);
                }
            }
        }

        public void NotContain(TItem match, string? message = null)
        {
            var comparer = EqualityComparer<TItem>.Default;
            NotContain((item) => comparer.Equals(item, match), message);
        }

        public void NotContainAnyOf(IEnumerable<TItem> expectation, string? message = null)
        {
            NullCheck(Subject, message);

            foreach (var item in expectation)
            {
                if (Subject.Contains(item) == true)
                {
                    throw new AssertionException($"Expected collection to not contain {item}", message);
                }
            }
        }


        public void NotContain(Func<TItem, bool> predicate, string? message = null)
        {
            if (Subject == null)
            {
                throw new AssertionException($"Expected collection to not contain item matching predicate, but it was null", message);
            }

            foreach (var item in Subject)
            {
                if (predicate(item) == false)
                {
                    return;
                }
            }

            throw new AssertionException($"Expected collection to not contain item matching predicate", message);

        }

        public void HaveCount(int count, string? message = null)
        {
            NullCheck(Subject, message);
            if (Subject.Count() != count)
            {
                throw new AssertionException($"Expected collection to have count {count}, but it had {Subject.Count()}", message);
            }
        }

        public ObjectAssertions<TItem> ContainSingle(Func<TItem, bool> test, string? message = null)
        {
            NullCheck(Subject, message);

            int count = 0;
            TItem? result = default;

            foreach (var item in Subject)
            {
                if (test(item))
                {
                    count += 1;
                    result = item;
                }
            }

            if (count != 1)
            {
                throw new AssertionException($"Expected collection to match a single item, but {count} did", message);
            }

            return new ObjectAssertions<TItem> { Subject = result! };
        }

    }

    public class DictionaryAssertions<TKey, TValue> : EnumerableAssertions<KeyValuePair<TKey, TValue>>
    {
        public ObjectAssertions<KeyValuePair<TKey, TValue>> ContainKey(TKey match, string? message = null)
        {
            NullCheck(Subject, message);

            var comparer = EqualityComparer<TKey>.Default;
            foreach (var kv in Subject)
            {
                if (comparer.Equals(kv.Key, match))
                {
                    return new ObjectAssertions<KeyValuePair<TKey, TValue>> { Subject = kv };
                }
            }

            throw new AssertionException($"Expected collection to contain a key matching {match}", message);
        }

        public ObjectAssertions<KeyValuePair<TKey, TValue>> ContainValue(TValue match, string? message = null)
        {
            NullCheck(Subject, message);

            var comparer = EqualityComparer<TValue>.Default;
            foreach (var kv in Subject)
            {
                if (comparer.Equals(kv.Value, match))
                {
                    return new ObjectAssertions<KeyValuePair<TKey, TValue>> { Subject = kv };
                }
            }

            throw new AssertionException($"Expected collection to contain a key matching {match}", message);
        }

        public ObjectAssertions<TKey> ContainKey(Func<TKey, bool> match, string? message = null)
        {
            NullCheck(Subject, message);
            foreach (var kv in Subject)
            {
                if (match(kv.Key))
                {
                    return new ObjectAssertions<TKey> { Subject = kv.Key };
                }
            }
            throw new AssertionException("Expected collection to contain a matching key", message);
        }

        public ObjectAssertions<TValue> ContainValue(Func<TValue, bool> match, string? message = null)
        {
            NullCheck(Subject, message);
            foreach (var kv in Subject)
            {
                if (match(kv.Value))
                {
                    return new ObjectAssertions<TValue> { Subject = kv.Value };
                }
            }
            throw new AssertionException("Expected collection to contain a matching value", message);
        }

        public void ContainItems(IEnumerable<KeyValuePair<TKey, TValue>> items, string? message = null)
        {
            ContainItems(items, null, message);
        }

        public void ContainItems(IEnumerable<KeyValuePair<TKey, TValue>> items, System.Func<TValue, TValue, bool>? comparer, string? message = null)
        {
            NullCheck(Subject, message);

            var keyComparer = EqualityComparer<TKey>.Default;

            if (comparer == null)
            {
                var valueComparer = EqualityComparer<TValue>.Default;
                comparer = (a, b) =>
                {
                    return valueComparer.Equals(a, b);
                };
            }
            foreach (var item in items)
            {
                var matchingElements = Subject.Where(kv => keyComparer.Equals(kv.Key, item.Key));
                if (matchingElements.Any() == false)
                {
                    // No keys in subject matched
                    throw new AssertionException($"Expected collection to have a value for \"{item.Key}\"", message);
                }

                var element = matchingElements.Single();

                if (comparer(element.Value, item.Value) == false)
                {
                    // The value for this key didn't match
                    throw new AssertionException($"Expected collection to have \"{item.Key}\" value \"{item.Value}\", not \"{element.Value}\" ", message);
                }
            }
        }
    }

    public class StringAssertions : ObjectAssertions<string>
    {
    }

    public static class AssertionExtensions
    {


        public static ObjectAssertions<object> Should(this object? subject)
        {
            return new ObjectAssertions<object>
            {
                Subject = subject
            };
        }
        public static BooleanAssertions Should(this bool subject)
        {
            return new BooleanAssertions { Subject = subject };
        }
        public static EnumerableAssertions<TItem> Should<TItem>(this IEnumerable<TItem> subject)
        {
            return new EnumerableAssertions<TItem> { Subject = subject };
        }
        public static DictionaryAssertions<TKey, TValue> Should<TKey, TValue>(this IDictionary<TKey, TValue> subject)
        {
            return new DictionaryAssertions<TKey, TValue> { Subject = subject };
        }
        public static NumericAssertions<int> Should(this int subject)
        {
            return new NumericAssertions<int> { Subject = subject };
        }
        public static NumericAssertions<float> Should(this float subject)
        {
            return new NumericAssertions<float> { Subject = subject };
        }
        public static NumericAssertions<double> Should(this double subject)
        {
            return new NumericAssertions<double> { Subject = subject };
        }
        public static StringAssertions Should(this string? subject)
        {
            return new StringAssertions { Subject = subject };
        }
    }


}
