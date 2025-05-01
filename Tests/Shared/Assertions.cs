/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.Assertions;

#nullable enable

namespace Yarn.Unity.Tests
{

    /// <summary>
    /// Contains assertions for objects.
    /// </summary>
    /// <typeparam name="TSubject">The type of object to assert
    /// against.</typeparam>
    public partial class ObjectAssertions<TSubject>
    {

        /// <summary>
        /// The subject of the assertions.
        /// </summary>
        [AllowNull]
        public TSubject Subject { get; internal set; }

        /// <summary>
        /// Checks to see if <paramref name="assertion"/> is <see
        /// langword="null"/>, and if it is, throws an exception.
        /// </summary>
        /// <param name="assertion">The object to test.</param>
        /// <param name="message">An optional message to include in the
        /// exception, if thrown.</param>
        /// <exception cref="AssertionException">Thrown if <paramref
        /// name="assertion"/> is <see langword="null"/>.</exception>
        protected static void NullCheck([NotNull] object? assertion, string? message)
        {
            if (assertion == null)
            {
                throw new AssertionException("Value is null", message);
            }
        }

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <exception cref="AssertionException">Thrown if <see cref="Subject"/>
        /// is not null.</exception>
        public void BeNull(string? message = null)
        {
            if (Subject != null)
            {
                throw new AssertionException($"Expected subject to be null, but it was non-null", message);
            }
        }

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is not <see langword="null"/>.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <exception cref="AssertionException">Thrown if <see cref="Subject"/>
        /// is null.</exception>
        public void NotBeNull(string? message = null)
        {
            if (Subject == null)
            {
                throw new AssertionException($"Expected subject to not be null, but it was null", message);
            }
        }

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is equal to another object.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <exception cref="AssertionException">Thrown if the <see
        /// cref="Subject"/> is not equal to <paramref
        /// name="other"/>.</exception>
        public void BeEqualTo(TSubject? other, string? message = null)
        {
            var comparer = EqualityComparer<TSubject>.Default;

            if (Subject == null && other == null)
            {
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

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is not equal to another
        /// object.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <exception cref="AssertionException">Thrown if the <see
        /// cref="Subject"/> is equal to <paramref name="other"/>.</exception>
        public void NotBeEqualTo(TSubject? other, string? message = null)
        {
            var comparer = EqualityComparer<TSubject>.Default;

            if (Subject == null && other != null
                || Subject != null && other == null)
            {
                // One of them is not null, so they're not equal
                return;
            }

            if ((Subject == null && other == null) || comparer.Equals(Subject, other!) == true)
            {
                throw new AssertionException($"Expected subject to not be equal to {other}, but it was {Subject}", message);
            }
        }

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is the same object as
        /// another.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>The current <see cref="ObjectAssertions{TSubject}"/>
        /// instance for method chaining.</returns>
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

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is not the same object as
        /// another.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>The current <see cref="ObjectAssertions{TSubject}"/>
        /// instance for method chaining.</returns>
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

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is of a type <typeparamref
        /// name="T"/>, or of a type assignable to <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to compare with the <see
        /// cref="Subject"/>.</typeparam>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>The current <see cref="ObjectAssertions{TSubject}"/>
        /// instance for method chaining.</returns>
        public ObjectAssertions<TSubject> BeOfType<T>(string? message = null)
        {
            NullCheck(Subject, message);

            if (typeof(T).IsAssignableFrom(Subject.GetType()) == false)
            {
                throw new AssertionException($"Expected subject to be {typeof(T)}, but it was {Subject.GetType()}", message);
            }

            return this;
        }

        /// <summary>
        /// Asserts that the <see cref="Subject"/> is of the exact type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to compare with the <see
        /// cref="Subject"/>.</typeparam>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{T}"/> object for this
        /// subject for method chaining.</returns>
        public ObjectAssertions<T> BeOfExactType<T>(string? message = null)
        {
            NullCheck(Subject, message);

            if (typeof(T) != Subject.GetType())
            {
                throw new AssertionException($"Expected subject to be exactly {typeof(T)}, but it was {Subject.GetType()}", message);
            }

            return new ObjectAssertions<T> { Subject = (T)(object)this.Subject };
        }
    }

    /// <summary>
    /// Contains assertions for boolean values.
    /// </summary>
    public partial class BooleanAssertions : ObjectAssertions<bool>
    {

        /// <summary>
        /// Asserts that the <see cref="ObjectAssertions{T}.Subject"/> is <see
        /// langword="true"/>.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeTrue(string? message = null)
        {
            if (Subject != true)
            {
                throw new AssertionException($"Expected subject to be true, but it was false", message);
            }
        }

        /// <summary>
        /// Asserts that the <see cref="ObjectAssertions{T}.Subject"/> is <see
        /// langword="false"/>.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeFalse(string? message = null)
        {
            if (Subject != false)
            {
                throw new AssertionException($"Expected subject to be false, but it was true", message);
            }
        }

    }

    /// <summary>
    /// Contains assertions for <see cref="IComparable"/> values.
    /// </summary>
    /// <typeparam name="TSubject">The type of the value.</typeparam>
    public class ComparableAssertions<TSubject> : ObjectAssertions<TSubject> where TSubject : IComparable<TSubject>
    {

        /// <summary>
        /// Asserts that the subject is greater than <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeGreaterThan(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) != 1)
            {
                throw new AssertionException($"Expected {Subject} to be greater than {other}", message);
            }
        }

        /// <summary>
        /// Asserts that the subject is less than <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeLessThan(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) != -1)
            {
                throw new AssertionException($"Expected {Subject} to be less than {other}", message);
            }
        }

        /// <summary>
        /// Asserts that the subject is greater than or equal to <paramref
        /// name="other"/>.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeGreaterThanOrEqualTo(TSubject other, string? message = null)
        {
            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) == -1)
            {
                throw new AssertionException($"Expected {Subject} to be greater than or equal to {other}", message);
            }
        }

        /// <summary>
        /// Asserts that the subject is less than or equal to <paramref
        /// name="other"/>.
        /// </summary>
        /// <param name="other">The object to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void BeLessThanOrEqualTo(TSubject other, string? message = null)
        {

            NullCheck(Subject, message);

            if (Comparer<TSubject>.Default.Compare(Subject, other) == 1)
            {
                throw new AssertionException($"Expected {Subject} to be less than or equal to {other}", message);
            }
        }
    }

    /// <summary>
    /// Contains assertions for numeric values.
    /// </summary>
    /// <typeparam name="TItem">The type of the value.</typeparam>
    public class NumericAssertions<TItem> : ComparableAssertions<TItem> where TItem : IComparable<TItem>
    {

    }

    /// <summary>
    /// Contains assertions for <see cref="IEnumerable{TItem}"/> values.
    /// </summary>
    /// <typeparam name="TItem">The type of the value.</typeparam>
    public class EnumerableAssertions<TItem> : ObjectAssertions<IEnumerable<TItem>>
    {


        /// <summary>
        /// Asserts that the collection is empty.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>The current <see cref="ObjectAssertions{TSubject}"/>
        /// instance for method chaining.</returns>
        public EnumerableAssertions<TItem> BeEmpty(string? message = null)
        {
            NullCheck(Subject, message);

            if (Subject.Count() != 0)
            {
                throw new AssertionException($"Expected collection to be empty, but it had {Subject.Count()} items", message);
            }
            return this;
        }

        /// <summary>
        /// Asserts that the collection is not empty.
        /// </summary>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>The current <see cref="ObjectAssertions{TSubject}"/>
        /// instance for method chaining.</returns>
        public EnumerableAssertions<TItem> NotBeEmpty(string? message = null)
        {
            NullCheck(Subject, message);

            if (Subject.Count() == 0)
            {
                throw new AssertionException($"Expected collection to not be empty", message);
            }
            return this;
        }


        /// <summary>
        /// Asserts that the collection contains an item matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match against items in the
        /// collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>A new <see cref="ObjectAssertions{TItem}"/> instance
        /// containing the matched item.</returns>
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

        /// <summary>
        /// Asserts that the collection contains a specific item.
        /// </summary>
        /// <param name="match">The item to match against items in the
        /// collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public ObjectAssertions<TItem> Contain(TItem match, string? message = null)
        {
            var comparer = EqualityComparer<TItem>.Default;
            return Contain((TItem item) => comparer.Equals(item, match), message);
        }

        /// <summary>
        /// Asserts that the colection contains exactly the specified items.
        /// </summary>
        /// <param name="expectation">The collection of items expected to be in
        /// the subject collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
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

        /// <summary>
        /// Asserts that the colection does not exactly contain the same items
        /// as the specified collection.
        /// </summary>
        /// <param name="expectation">The collection of items to test
        /// against.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void NotContainExactly(IEnumerable<TItem> expectation, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(expectation, message);

            bool exactMatch = true;

            foreach (var item in Subject)
            {
                if (expectation.Contains(item) == false)
                {
                    exactMatch = false;
                    break;
                }
            }
            foreach (var item in expectation)
            {
                if (Subject.Contains(item) == false)
                {
                    exactMatch = false;
                    break;
                }
            }
            if (exactMatch)
            {
                throw new AssertionException($"Expected collection to not contain exact same items as {expectation}", message);
            }
        }

        /// <summary>
        /// Asserts that the collection contains all of the specified items.
        /// </summary>
        /// <param name="expectation">The collection of items expected to be in
        /// the subject collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
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

        /// <summary>
        /// Asserts that the collection does not contain a specific item.
        /// </summary>
        /// <param name="match">The item that is expected to be absent from the
        /// subject collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void NotContain(TItem match, string? message = null)
        {
            var comparer = EqualityComparer<TItem>.Default;
            NotContain((item) => comparer.Equals(item, match), message);
        }

        /// <summary>
        /// Asserts that the collection does not contain any of the specified
        /// items.
        /// </summary>
        /// <param name="expectation">The collection of items expected to be
        /// absent from the subject collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
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

        /// <summary>
        /// Asserts that the collection does not contain an item matching a
        /// given predicate.
        /// </summary>
        /// <param name="predicate">A function to test each item in the
        /// collection for a condition.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
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

        /// <summary>
        /// Asserts that the collection has a specific number of items.
        /// </summary>
        /// <param name="count">The expected number of items in the subject
        /// collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void HaveCount(int count, string? message = null)
        {
            NullCheck(Subject, message);
            if (Subject.Count() != count)
            {
                throw new AssertionException($"Expected collection to have count {count}, but it had {Subject.Count()}", message);
            }
        }

        /// <summary>
        /// Asserts that the subject collection contains exactly one item
        /// matching a specified condition.
        /// </summary>
        /// <param name="test">A function delegate to test each item in the
        /// collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{TItem}"/> with the single
        /// matching item as its subject.</returns>
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

        /// <summary>
        /// Asserts that the subject collection contains items equal to those in
        /// other, and in the same order.
        /// </summary>
        /// <param name="other">The collection to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public new void BeEqualTo(IEnumerable<TItem> other, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(other, message);

            var comparer = EqualityComparer<TItem>.Default;

            if (Subject.Count() != other.Count())
            {
                throw new AssertionException($"Expected {Subject} to be equal to {other}, but they don't have the same count ({Subject.Count()} vs {other.Count()})", message);
            }

            for (int i = 0; i < Subject.Count(); i++)
            {
                var item1 = Subject.ElementAt(i);
                var item2 = other.ElementAt(i);

                if (comparer.Equals(item1, item2) == false)
                {
                    throw new AssertionException($"Expected {Subject} to be equal to {other}, but they differ at element {i} ({item1} vs {item2})", message);
                }
            }
        }

        /// <summary>
        /// Asserts that the subject collection does not contain items equal to
        /// those in other in the same order.
        /// </summary>
        /// <param name="other">The collection to compare with the <see
        /// cref="ObjectAssertions{T}.Subject"/>.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public new void NotBeEqualTo(IEnumerable<TItem> other, string? message)
        {
            NullCheck(Subject, message);
            NullCheck(other, message);

            try
            {
                BeEqualTo(other);
            }
            catch (AssertionException)
            {
                return;
            }

            throw new AssertionException($"Expected {Subject} to not be equal to {other}", message);
        }
    }

    /// <summary>
    /// Contains assertions for dictionaries.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the
    /// dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the
    /// dictionary.</typeparam>
    public class DictionaryAssertions<TKey, TValue> : EnumerableAssertions<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// Asserts that the subject dictionary contains an entry with the
        /// specified key.
        /// </summary>
        /// <param name="match">The key to search for in the collection</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{TItem}"/> with the single
        /// matching item as its subject.</returns>
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

        /// <summary>
        /// Asserts that the subject dictionary contains an entry with the
        /// specified value.
        /// </summary>
        /// <param name="match">The value to search for in the
        /// collection</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{TItem}"/> with the single
        /// matching item as its subject.</returns>
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

            throw new AssertionException($"Expected collection to contain a value matching {match}", message);
        }

        /// <summary>
        /// Asserts that the subject dictionary contains an entry with a key
        /// that matches the specified condition.
        /// </summary>
        /// <param name="match">A function to test each key in the
        /// collection</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{TItem}"/> with the matching
        /// item as its subject.</returns>
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

        /// <summary>
        /// Asserts that the subject dictionary contains an entry with a value
        /// that matches the specified condition.
        /// </summary>
        /// <param name="match">A function to test each value in the
        /// collection</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        /// <returns>An <see cref="ObjectAssertions{TItem}"/> with the matching
        /// item as its subject.</returns>
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

        /// <summary>
        /// Asserts that the subject dictionary contains all specified key-value
        /// pairs.
        /// </summary>
        /// <param name="items">The key-value pairs to check for in the
        /// collection.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void ContainItems(IEnumerable<KeyValuePair<TKey, TValue>> items, string? message = null)
        {
            ContainItems(items, null, message);
        }

        /// <summary>
        /// Asserts that the subject dictionary contains all specified key-value
        /// pairs.
        /// </summary>
        /// <param name="items">The key-value pairs to check for in the
        /// collection.</param>
        /// <param name="comparer">An optional function to compare values. If
        /// this is null, <see cref="EqualityComparer{TValue}"/> is
        /// used.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
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

    /// <summary>
    /// Contains assertions for strings.
    /// </summary>
    public class StringAssertions : ObjectAssertions<string>
    {
        /// <summary>
        /// Asserts that the subject string contains a substring, using the
        /// specified string comparison type.
        /// </summary>
        /// <param name="substring">The substring to check for in the
        /// string.</param>
        /// <param name="comparison">The type of string comparison to
        /// perform.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void Contain(string substring, StringComparison comparison, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(substring, message);
            if (Subject.Contains(substring, comparison) == false)
            {
                throw new AssertionException($"Expected string \"{Subject}\" to contain substring \"{substring}\" (using comparison {comparison})", message);
            }
        }

        /// <summary>
        /// Asserts that the subject string contains a substring, using a
        /// case-sensitive, culture-insensitive string comparison.
        /// </summary>
        /// <param name="substring">The substring to check for in the
        /// string.</param>
        /// <param name="message">An optional message to include if the
        /// assertion fails.</param>
        public void Contain(string substring, string? message = null)
        {
            Contain(substring, StringComparison.InvariantCulture, message);
        }

        public void Match(System.Text.RegularExpressions.Regex regex, string? message = null)
        {
            NullCheck(Subject, message);
            NullCheck(regex, message);

            if (regex.IsMatch(this.Subject) == false)
            {
                throw new AssertionException($"Expected string \"{Subject}\" to match regex substring \"{regex}\"", message);
            }
        }
    }


    /// <summary>
    /// Adds the Should() method to supported types.
    /// </summary>
    public static class AssertionExtensions
    {


        /// <summary>
        /// Gets an assertions object for the given subject.
        /// </summary>
        /// <param name="subject">The object to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static ObjectAssertions<object> Should(this object? subject)
        {
            if (subject is UnityEngine.Object unityObject)
            {
                // Special-case Unity objects: they override the equality
                // operator so that 'x == null' may be true if x is not null but
                // no longer represents an object in memory (aghhhh). This
                // causes problems with later checks, because our code doesn't
                // know that it should use the overloaded == operator.
                //
                // To get around this, we'll perform the 'sort-of-null' check
                // here, and if they're in this state, act like the user passed
                // in an actual 'null' value.

                if (!unityObject)
                {
                    return new ObjectAssertions<object> { Subject = null };
                }
            }
            return new ObjectAssertions<object> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given boolean subject.
        /// </summary>
        /// <param name="subject">The boolean value to create an assertions
        /// object for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static BooleanAssertions Should(this bool subject)
        {
            return new BooleanAssertions { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given enumerable subject.
        /// </summary>
        /// <typeparam name="TItem">The type of elements in the
        /// enumerable.</typeparam>
        /// <param name="subject">The enumerable to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static EnumerableAssertions<TItem> Should<TItem>(this IEnumerable<TItem> subject)
        {
            return new EnumerableAssertions<TItem> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given dictionary subject.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the
        /// dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the
        /// dictionary.</typeparam>
        /// <param name="subject">The dictionary to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static DictionaryAssertions<TKey, TValue> Should<TKey, TValue>(this IDictionary<TKey, TValue> subject)
        {
            return new DictionaryAssertions<TKey, TValue> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given integer subject.
        /// </summary>
        /// <param name="subject">The integer value to create an assertions
        /// object for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static NumericAssertions<int> Should(this int subject)
        {
            return new NumericAssertions<int> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given <see langword="float"/>
        /// subject.
        /// </summary>
        /// <param name="subject">The float value to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static NumericAssertions<float> Should(this float subject)
        {
            return new NumericAssertions<float> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given <see langword="double"/>
        /// subject.
        /// </summary>
        /// <param name="subject">The double value to create an assertions
        /// object for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static NumericAssertions<double> Should(this double subject)
        {
            return new NumericAssertions<double> { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given string subject.
        /// </summary>
        /// <param name="subject">The string to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static StringAssertions Should(this string? subject)
        {
            return new StringAssertions { Subject = subject };
        }

        /// <summary>
        /// Gets an assertions object for the given enum subject.
        /// </summary>
        /// <param name="subject">The enum to create an assertions object
        /// for.</param>
        /// <returns>An assertions object for <paramref
        /// name="subject"/>.</returns>
        public static ObjectAssertions<T> Should<T>(this T subject) where T : struct, Enum
        {
            return new ObjectAssertions<T> { Subject = subject };
        }
    }


}
