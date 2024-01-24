/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
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
