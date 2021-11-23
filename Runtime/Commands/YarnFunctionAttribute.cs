using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Marks the method as a function to be registered with the running
    /// instance's library.
    /// </summary>
    /// <remarks>
    /// See <see cref="Library.RegisterFunction(string, Delegate)"/> and the
    /// generic overloads for what is and is not valid.
    /// 
    /// This will throw an error if you attempt to add a function that has
    /// more than 16 parameters, as that is the largest overload that
    /// <see cref="Func{TResult}"/> etc has.
    /// </remarks>
    public class YarnFunctionAttribute : YarnActionAttribute {
        [Obsolete("Use " + nameof(Name) + " instead.")]
        public string FunctionName
        {
            get => Name;
            set => Name = value;
        }

        public YarnFunctionAttribute(string name = null) => Name = name;
    }
}
