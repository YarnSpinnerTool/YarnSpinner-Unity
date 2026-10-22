/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    #region Class/Interface

    /// <summary>
    /// An attribute that marks a method on an object as a command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is identified by a source generator and used to generate a file that performs the necessary action validation, registration, and parameter conversion for the command to be callable through your Yarn scripts.
    /// The specific behaviour of command changes drastically depending on the method signature.
    /// For full details of this see <see cref="https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions"/> which covers this nuance.
    /// </para>
    /// </remarks>
    [HelpURL("https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions")]
    public class YarnCommandAttribute : YarnActionAttribute
    {
        /// <inheritdoc/>
        public YarnCommandAttribute(string? name = null) => Name = name;
    }
    #endregion
}
