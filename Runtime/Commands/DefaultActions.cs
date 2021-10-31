using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    internal class DefaultActions : MonoBehaviour
    {
        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Unity.
        /// </summary>
        /// <param name="duration">How long to wait.</param>
        [YarnCommand]
        public static IEnumerator wait(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
        #endregion

        #region Functions
        [YarnFunction]
        public static float random()
        {
            return random_range(0, 1);
        }

        [YarnFunction]
        public static float random_range(float minInclusive, float maxInclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxInclusive);
        }

        /// <summary>
        /// Pick an integer in the given range.
        /// </summary>
        /// <param name="sides">Dice range.</param>
        /// <returns>A number between <c>[1, <paramref name="sides"/>]</c>.
        /// </returns>
        [YarnFunction]
        public static int dice(int sides)
        {
            return UnityEngine.Random.Range(1, sides + 1);
        }

        [YarnFunction]
        public static int round(float num)
        {
            return (int)round_places(num, 0);
        }

        [YarnFunction]
        public static float round_places(float num, int places)
        {
            return (float)Math.Round(num, places);
        }

        [YarnFunction]
        public static int floor(float num)
        {
            return Mathf.FloorToInt(num);
        }

        [YarnFunction]
        public static int ceil(float num)
        {
            return Mathf.CeilToInt(num);
        }

        /// <summary>
        /// Increment if integer, otherwise go to next integer.
        /// </summary>
        [YarnFunction]
        public static int inc(float num)
        {
            if (@decimal(num) != 0)
            {
                return Mathf.CeilToInt(num);
            }
            return (int)num + 1;
        }

        /// <summary>
        /// Decrement if integer, otherwise go to previous integer.
        /// </summary>
        [YarnFunction]
        public static int dec(float num)
        {
            if (@decimal(num) != 0)
            {
                return Mathf.FloorToInt(num);
            }
            return (int)num - 1;
        }

        /// <summary>
        /// The decimal portion of the given number.
        /// </summary>
        /// <param name="num">Number to get the decimal portion of.</param>
        /// <returns><c>[0, 1)</c></returns>
        [YarnFunction]
        public static float @decimal(float num)
        {
            return num - @int(num);
        }

        /// <summary>
        /// Truncates the number into an int. This is different to
        /// <see cref="floor(float)"/> because it rounds to zero rather than
        /// <see cref="Mathf.NegativeInfinity"/>.
        /// </summary>
        /// <param name="num">Number to truncate.</param>
        /// <returns>Truncated float value as int.</returns>
        [YarnFunction]
        public static int @int(float num)
        {
            return (int)Math.Truncate(num);
        }
        #endregion
    }
}
