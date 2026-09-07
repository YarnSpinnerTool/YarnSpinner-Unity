/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Attributes
{
    /// <summary>
    /// The different types of ways a <see cref="YarnNodeAttribute"/> can filter
    /// its node list in the Inspector.
    /// </summary>
    /// <remarks>The filter text is case-insensitive for all filter types except
    /// <see cref="YarnNodeFilter.MatchesRegex"/> .</remarks>
    public enum YarnNodeFilter
    {
        /// <summary>
        /// Do not filter the node list.
        /// </summary>
        None,
        /// <summary>
        /// Filter the node list to include nodes where the filtered header
        /// contains the filter text.
        /// </summary>
        Contains,
        /// <summary>
        /// Filter the node list to include nodes where the filtered header
        /// starts with the filter text.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Filter the node list to include nodes where the filtered header ends
        /// with the filter text.
        /// </summary>
        EndsWith,
        /// <summary>
        /// Filter the node list to include nodes where a regex defined by the
        /// filter text matches the filtered header.
        /// </summary>
        MatchesRegex
    }

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
        /// The name of a property that specifies the YarnProject containing the
        /// desired node.
        /// </summary>
        public readonly string? yarnProjectAttribute;

        /// <summary>
        /// The text that specifies the Nodes you want to include in the
        /// dropdown.
        /// </summary>
        /// <remarks>The filter text is case-insensitive for all filter types
        /// except <see cref="YarnNodeFilter.MatchesRegex"/> .</remarks>
        public readonly string? filter;

        /// <summary>
        /// The filter type you'd like to use to find Nodes.
        /// </summary>
        public readonly YarnNodeFilter filterType = YarnNodeFilter.Contains;

        /// <summary>
        /// The header you want to apply filtering to. Defaults to 'title'.
        /// </summary>
        public readonly string? filterHeader = null;

        /// <summary>
        /// Controls the behaviour of this property when the Yarn Project is not
        /// set. If true, the property will be shown as a disabled empty list.
        /// If false, the property will be shown as a free-form text field.
        /// </summary>
        public readonly bool requiresYarnProject;

        /// <summary>
        /// Initialises a new instance of <see cref="YarnNodeAttribute"/>.
        /// </summary>
        /// <param name="yarnProjectAttribute"><inheritdoc
        /// cref="yarnProjectAttribute" path="/summary/node()"/></param>
        /// <param name="filter"><inheritdoc cref="filter"
        /// path="/summary/node()" /></param>
        /// <param name="requiresYarnProject"><inheritdoc cref="filter"
        /// path="/summary/node()" /></param>
        /// <param name="filterHeader"><inheritdoc cref="filterHeader"
        /// path="/summary/node()" /></param>
        /// <param name="filterType"><inheritdoc cref="filterType"
        /// path="/summary/node()" /></param>
        public YarnNodeAttribute(string yarnProjectAttribute, bool requiresYarnProject = true, string? filter = default, YarnNodeFilter filterType = YarnNodeFilter.Contains, string filterHeader = "title")
        {
            this.yarnProjectAttribute = yarnProjectAttribute;
            this.requiresYarnProject = requiresYarnProject;
            this.filter = filter;
            this.filterType = filterType;
            this.filterHeader = filterHeader;
        }
    }
}
