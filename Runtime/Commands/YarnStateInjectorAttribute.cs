/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using System;

namespace Yarn.Unity
{
    /// <summary>
    /// Inject state for any commands in this class using this static method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method will be expected to take a string, and return an entity
    /// of the same type as the class being operated on. (So, for example,
    /// an injector for <see cref="BoxCollider"/> would take in a string
    /// and find a <see cref="BoxCollider"/>.)
    /// </para>
    /// <para>
    /// By default, Yarn will use <see cref="GameObject.Find(string)"/>
    /// if there is no injector defined. This is fairly inefficient (an
    /// <c>O(n)</c> lookup), so it is recommended that you restrict your
    /// lookup conditions so that you can find it quicker (eg, a cache).
    /// </para>
    /// <para>
    /// This injector should be a static function. Non-static functions
    /// will be ignored.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class YarnStateInjectorAttribute : Attribute {
        /// <summary>
        /// Method to use as an injector.
        /// </summary>
        /// <remarks>
        /// Can be overridden per-method using <see cref="YarnCommandAttribute.Injector"/>.
        /// </remarks>
        public string Injector { get; set; }

        public YarnStateInjectorAttribute(string injector) => Injector = injector;
    }
}
