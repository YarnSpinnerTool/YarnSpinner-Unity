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
