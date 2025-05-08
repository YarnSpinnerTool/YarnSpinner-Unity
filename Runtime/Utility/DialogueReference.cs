/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Stores a reference to a dialogue node in a Yarn Project.
    /// </summary>
    /// <remarks>
    /// A Dialogue Reference is a reference to a named node inside a given Yarn
    /// Project. This allows the editor to warn the user if node doesn't exist
    /// in the specified project.
    /// </remarks>
    [Serializable]
    public sealed class DialogueReference
    {
        /// <summary>
        /// The Yarn Project asset containing the dialogue node.
        /// </summary>
        public YarnProject? project;

        /// <summary>
        /// The name of the dialogue node in the project.
        /// </summary>
        [YarnNode(nameof(project), requiresYarnProject: false)]
        public string? nodeName;

        /// <summary>
        /// Gets a value indicating that this reference is valid - that is, the
        /// project and node name are set, and the node exists in the project.
        /// </summary>
        public bool IsValid => project != null && !string.IsNullOrEmpty(nodeName) && project.Program.Nodes.ContainsKey(nodeName);

        /// <summary>
        /// Creates an empty dialogue reference.
        /// </summary>
        public DialogueReference() { }

        /// <summary>
        /// Creates a dialogue reference with a given project and node name.
        /// </summary>
        /// <param name="project">Yarn Project asset containing the
        /// node.</param>
        /// <param name="nodeName">Name of the node in the project
        /// asset.</param>
        public DialogueReference(YarnProject project, string nodeName)
        {
            this.project = project;
            this.nodeName = nodeName;
        }

        // DialogueReferences can be implicitly converted to strings
        public static implicit operator string?(DialogueReference reference)
        {
            return reference.nodeName;
        }
    }
}
