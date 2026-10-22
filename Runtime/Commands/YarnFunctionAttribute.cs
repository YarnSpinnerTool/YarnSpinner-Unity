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
    /// Marks the method as a function to be registered with the dialogue runner.
    /// </summary>
    /// <remarks>
    /// This attribute is identified by a source generator and used to generate a file that performs the necessary action validation, registration, and parameter conversion for the function to be callable through your Yarn scripts.
    /// The specific behaviour of function changes drastically depending on the method signature.
    /// For full details of this see <see cref="https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions"/> which covers this nuance.
    /// </remarks>
    public class YarnFunctionAttribute : YarnActionAttribute
    {
        public YarnFunctionAttribute(string? name = null) => Name = name;
    }
}
