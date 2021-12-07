using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Yarn parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class YarnParameterAttribute : Attribute
    {
        /// <summary>
        /// The custom injector for this parameter.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="YarnCommandAttribute.Injector"/>.
        /// 
        /// If this is not defined on a parameter, or if the injector is not
        /// static or doesn't exist, or returns a type that is not assignable
        /// to the parameter's type, then it will be use the default method,
        /// which is to search for the game object name, and search for the
        /// target component type on that object.
        /// </remarks>
        public string Injector { get; set; }

        public YarnParameterAttribute(string injector) => Injector = injector;
    }
}
