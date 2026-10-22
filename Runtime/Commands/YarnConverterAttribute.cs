/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using System;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Attribute that marks a command as being a converter for a type used in commands and functions.
    /// </summary>
    /// <remarks>
    /// Methods flagged with this attribute must:
    /// <list type="bullet">
    /// <item>be static</item>
    /// <item>be public</item>
    /// <item>return bool</item>
    /// <item>have a string parameter</item>
    /// <item>have an out parameter that matches the type of the attributes <see cref="ConversionSupport"/> type</item>
    /// </list>
    /// For full details of see <see cref="https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions"/> which covers the nuance of using converters in your actions.
    /// </remarks>
    [HelpURL("https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions")]
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class YarnConverterAttribute : Attribute
    {
        public Type ConversionSupport { get; set; }

        public YarnConverterAttribute(Type type) => ConversionSupport = type;
    }
}
