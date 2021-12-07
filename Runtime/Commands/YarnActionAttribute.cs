using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class YarnActionAttribute : Attribute
    {
        /// <summary>
        /// The name of the command or function, as it exists in Yarn.
        /// </summary>
        /// <remarks>
        /// This value does not have to be the same as the name of the
        /// method. For example, you could have a method named
        /// "`WalkToPoint`", and expose it to Yarn as a command named
        /// "`walk_to_point`".
        /// </remarks>
        public string Name { get; set; }

        public YarnActionAttribute(string name = null) => Name = name;
    }
}
