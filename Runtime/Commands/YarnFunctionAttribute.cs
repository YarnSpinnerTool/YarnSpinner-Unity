/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Marks the method as a function to be registered with the running
    /// instance's library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See <see cref="Library.RegisterFunction(string, Delegate)"/> and the
    /// generic overloads for what is and is not valid.
    /// </para>
    /// <para>
    /// This will throw an error if you attempt to add a function that has
    /// more than 16 parameters, as that is the largest overload that
    /// <see cref="Func{TResult}"/> etc has.
    /// </para>
    /// <para>
    /// Yarn Spinner for Unity finds methods with the YarnFunction attribute by
    /// reading your source code. If your project uses Unity 2021.1 or earlier,
    /// you will need to tell Yarn Spinner for Unity to do this manually, by
    /// opening the Window method and choosing Yarn Spinner -&gt; Update Yarn
    /// Commands. You don't need to do this on later versions of Unity, as it
    /// will be done for you automatically when your code compiles.
    /// </para>
    /// </remarks>
    public class YarnFunctionAttribute : YarnActionAttribute
    {
        public YarnFunctionAttribute(string? name = null) => Name = name;
    }
}
