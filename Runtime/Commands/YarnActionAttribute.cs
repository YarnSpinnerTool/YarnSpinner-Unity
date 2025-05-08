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
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public abstract class YarnActionAttribute : Attribute
    {
        /// <summary>
        /// The name of the command or function, as it exists in Yarn.
        /// </summary>
        /// <remarks>
        /// This value does not have to be the same as the name of the method.
        /// For example, you could have a method named "`WalkToPoint`", and
        /// expose it to Yarn as a command named "`walk_to_point`".
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="YarnActionAttribute"/>
        /// class.
        /// </summary>
        /// <param name="name">The name of the action. If not provided or <see
        /// langword="null"/>, the name of the method is used instead.</param>
        public YarnActionAttribute(string? name = null) => Name = name;
    }
}
