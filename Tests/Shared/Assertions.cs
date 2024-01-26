
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

#nullable enable

namespace Yarn.Unity.Tests
{

    public class ShouldAssertion<T>
    {

        public T? Subject { get; private set; }
        public ShouldAssertion(T? subject)
        {
            this.Subject = subject;
        }
    }

    public static class AssertionExtensions
    {
        public static ShouldAssertion<T> Should<T>(this T subject)
        {
            return new ShouldAssertion<T>(subject);
        }

        public static void BeTrue(this ShouldAssertion<bool> value, string? message = null)
        {
            if (value.Subject != true)
            {
                throw new AssertionException($"Expected subject to be true, but it was false", message);
            }
        }

        public static void BeFalse(this ShouldAssertion<bool> value, string? message = null)
        {
            if (value.Subject != false)
            {
                throw new AssertionException($"Expected subject to be false, but it was true", message);
            }
        }

        public static void BeNull<T>(this ShouldAssertion<T> value, string? message = null) where T : class?
        {
            if (value.Subject != null)
            {
                throw new AssertionException($"Expected subject to be null, but it was non-null", message);
            }
        }

        public static void NotBeNull<T>(this ShouldAssertion<T> value, string? message = null) where T : class?
        {
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected subject to not be null, but it was null", message);
            }
        }

        public static void BeEqualTo<T>(this ShouldAssertion<T> value, T? other, string? message = null)
        {
            var comparer = EqualityComparer<T>.Default;

            if (value.Subject == null || other == null)
            {
                if (value.Subject == null && other == null)
                {
                    // They're equal, because they're both null
                    return;
                }
                else
                {
                    throw new AssertionException($"Expected subject to be equal to {other ?? value.Subject ?? throw new NullReferenceException("both values were somehow null")}, but it was {value.Subject}", message);
                }
            }

            if (comparer.Equals(value.Subject, other) != true)
            {
                throw new AssertionException($"Expected subject to be equal to {other}, but it was {value.Subject}", message);
            }
        }

        public static void NotBeEqualTo<T>(this ShouldAssertion<T> value, T? other, string? message = null)
        {
            var comparer = EqualityComparer<T>.Default;
            if (value.Subject == null && value.Subject == null)
            {
                throw new AssertionException($"Expected two values not to equal each other, but they were both null", message);
            }
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected value to not be equal to {other}, but value was null", message);
            }
            if (other == null)
            {
                throw new AssertionException($"Expected value to not be equal to {value.Subject}, but value was null", message);
            }
            if (comparer.Equals(value.Subject, other) == true)
            {
                throw new AssertionException($"Expected subject to not be equal to {value}, but it was {value.Subject}", message);
            }
        }

        public static void BeEmpty<T>(this ShouldAssertion<IEnumerable<T>> value, string? message = null)
        {
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected collection to be empty, but it was null", message);
            }

            if (value.Subject.Count() != 0)
            {
                throw new AssertionException($"Expected collection to be empty, but it had {value.Subject.Count()} items", message);
            }
        }
        public static void NotBeEmpty<T>(this ShouldAssertion<IEnumerable<T>> value, string? message = null)
        {
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected collection to not be empty, but it was null", message);
            }

            if (value.Subject.Count() == 0)
            {
                throw new AssertionException($"Expected collection to not be empty", message);
            }
        }

        public static void Contain<T>(this ShouldAssertion<IEnumerable<T>> value, T match, string? message = null)
        {
            var comparer = EqualityComparer<T>.Default;
            Contain(value, (item) => comparer.Equals(item, match), message);
        }

        public static void Contain<T,U>(this ShouldAssertion<T> value, IEnumerable<U> expectation, string? message = null) where T : IEnumerable<U>
        {
            var enumerable = value.Subject as IEnumerable<U>;

            foreach (var item in expectation)
            {
                if (enumerable.Contains(item) == false)
                {
                    throw new AssertionException($"Expected collection to contain {item}", message);
                }
            }
        }

        public static void NotContain<T>(this ShouldAssertion<IEnumerable<T>> value, T match, string? message = null)
        {
            var comparer = EqualityComparer<T>.Default;
            NotContain(value, (item) => comparer.Equals(item, match), message);
        }

        public static void NotContain<T,U>(this ShouldAssertion<T> value, IEnumerable<U> expectation, string? message = null) where T : IEnumerable<U>
        {
            var enumerable = value.Subject as IEnumerable<U>;

            foreach (var item in expectation)
            {
                if (enumerable.Contains(item) == true)
                {
                    throw new AssertionException($"Expected collection to not contain {item}", message);
                }
            }
        }

        public static void Contain<T>(this ShouldAssertion<IEnumerable<T>> value, Func<T, bool> predicate, string? message = null)
        {
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected collection to contain item matching predicate, but it was null", message);
            }

            foreach (var item in value.Subject)
            {
                if (predicate(item) == true)
                {
                    return;
                }
            }

            throw new AssertionException($"Expected collection to contain item matching predicate", message);

        }

        public static void NotContain<T>(this ShouldAssertion<IEnumerable<T>> value, Func<T, bool> predicate, string? message = null)
        {
            if (value.Subject == null)
            {
                throw new AssertionException($"Expected collection to not contain item matching predicate, but it was null", message);
            }

            foreach (var item in value.Subject)
            {
                if (predicate(item) == false)
                {
                    return;
                }
            }

            throw new AssertionException($"Expected collection to not contain item matching predicate", message);

        }

        public static void HaveCount<T>(this ShouldAssertion<T> value, int count, string? message = null) where T : class, IEnumerable<object>
        {
            var enumerable = value.Subject;
            if (enumerable.Count() != count) {
                throw new AssertionException($"Expected collection to have count {count}", message);
            }
        }
    }
}
