/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;

namespace Yarn.Unity.Attributes
{
    public enum YarnNodeFilter { None, Contains, Start, End }

    /// <summary>
    /// Specifies that a field represents a reference to a named Yarn node that
    /// exists in a Yarn project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute causes the inspector to draw a popup that allows
    /// selecting a node from a list of all nodes available in a Yarn project.
    /// </para>
    /// <para>
    /// This attribute may only be used with <see cref="string"/> fields.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class YarnNodeAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of a property that specifies the YarnProject containing the desired node.
        /// </summary>
        public readonly string yarnProjectAttribute;

        /// <summary>
        /// The text that specifies the Nodes you want to include in the dropdown.
        /// </summary>
        public readonly string filter;

        /// <summary>
        /// The filter type you'd like to use to find Nodes.
        /// </summary>
        public readonly YarnNodeFilter filterType;

        public readonly bool requiresYarnProject;

        /// <summary>
        /// Initialises a new instance of <see cref="YarnNodeAttribute"/>.
        /// </summary>
        /// <param name="yarnProjectAttribute"><inheritdoc
        /// cref="yarnProjectAttribute" path="/summary/node()"/></param>
        public YarnNodeAttribute(string yarnProjectAttribute, bool requiresYarnProject = true, string filter = default, YarnNodeFilter filterType = YarnNodeFilter.Contains)
        {
            this.yarnProjectAttribute = yarnProjectAttribute;
            this.requiresYarnProject = requiresYarnProject;
            this.filter = filter;
            this.filterType = filterType;
        }
    }
}
