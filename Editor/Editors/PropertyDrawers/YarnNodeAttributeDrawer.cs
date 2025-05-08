/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Property drawer for <see cref="DialogueReference"/>
    /// </summary>
    [CustomPropertyDrawer(typeof(YarnNodeAttribute))]
    public class YarnNodeAttributeDrawer : PropertyDrawer
    {
        private const string NodeTextControlNamePrefix = "DialogueReference.NodeName.";

        private YarnProject? lastProject;
        private string? lastNodeName;
        private bool referenceExists;
        private bool editNodeAsText;
        private bool focusNodeTextField;

        private GUIContent? nodenameContent;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // -- Yarn Project asset reference
            SerializedProperty projectProp;

            if (this.attribute is not YarnNodeAttribute attribute)
            {
                throw new System.InvalidOperationException($"Internal error: attribute is not a {nameof(YarnNodeAttribute)}");
            }

            var propertyPathComponents = new System.Collections.Generic.Stack<string>(property.propertyPath.Split('.'));

            while (true)
            {
                string testPath;

                if (propertyPathComponents.Count == 0)
                {
                    testPath = attribute.yarnProjectAttribute;
                }
                else
                {
                    var components = new System.Collections.Generic.List<string>(propertyPathComponents);
                    components.Reverse();

                    testPath = string.Join(".", components) + "." + attribute.yarnProjectAttribute;
                }

                projectProp = property.serializedObject.FindProperty(testPath);

                if (projectProp != null)
                {
                    break;
                }

                if (propertyPathComponents.Count > 0)
                {
                    propertyPathComponents.Pop();
                }
                else
                {
                    break;
                }
            }

            if (projectProp == null)
            {
                EditorGUI.HelpBox(position, $"{attribute.yarnProjectAttribute} does not exist on {property.serializedObject.targetObject.name}", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            position = EditorGUI.PrefixLabel(position, controlId, label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var nodeNameFieldPosition = position;

            var project = projectProp.hasMultipleDifferentValues ? null : projectProp.objectReferenceValue as YarnProject;

            // -- Node name drop down

            // If we want to edit this nodes name as a text field, or if we have
            // multiple values, or if we have no project and we don't need one,
            // show a text field and not a dropdown.
            if ((project == null && attribute.requiresYarnProject == false) || editNodeAsText || projectProp.hasMultipleDifferentValues)
            {
                var controlName = NodeTextControlNamePrefix + controlId;

                // Multi-selection with different projects, just show a text
                // field to edit the node name. Most of the time, it will show
                // the mixed value dash (—).
                GUI.SetNextControlName(controlName);

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var currentText = property.hasMultipleDifferentValues ? "-" : property.stringValue;
                    currentText = EditorGUI.TextField(nodeNameFieldPosition, currentText);
                    if (change.changed)
                    {
                        property.stringValue = currentText;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }

                if (editNodeAsText)
                {
                    if (focusNodeTextField)
                    {
                        // Focusing the text field is delayed like this because
                        // the control needs to exist first before we can focus
                        // it
                        focusNodeTextField = false;
                        EditorGUI.FocusTextInControl(controlName);
                    }
                    else if (ShouldEndEditing(controlName))
                    {
                        editNodeAsText = false;
                        HandleUtility.Repaint();
                    }
                }
            }
            else
            {
                // Show a dropdown that lets the user choose a node from the
                // ones present in the Yarn Project.

                // If the Yarn Project is not set, this dropdown is empty and
                // disabled.
                if (project == null)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUI.DropdownButton(nodeNameFieldPosition, GUIContent.none, FocusType.Passive);
                    }
                }
                else
                {
                    var nodeName = property.stringValue;
                    var nodeNameSet = !string.IsNullOrEmpty(nodeName);

                    // Cached check if node exists in project
                    if (lastProject != project || lastNodeName != nodeName)
                    {
                        lastProject = project;
                        lastNodeName = nodeName;
                        referenceExists = project.Program.Nodes.ContainsKey(nodeName);
                    }

                    if (nodenameContent == null)
                    {
                        nodenameContent = new GUIContent();
                    }

                    // Show warning icon if not does not exist in selected project

                    if (nodeNameSet)
                    {
                        nodenameContent.text = nodeName;
                    }
                    else
                    {
                        nodenameContent.text = "(Choose Node)";
                    }

                    MessageType iconType = MessageType.None;

                    if (!nodeNameSet)
                    {
                        iconType = MessageType.Info;
                    }
                    else if (!referenceExists)
                    {
                        iconType = MessageType.Warning;
                    }
                    else
                    {
                        iconType = MessageType.None;
                    }

                    switch (iconType)
                    {
                        case MessageType.Info:
                            nodenameContent.image = EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_console.infoicon.sml").image : EditorGUIUtility.IconContent("console.infoicon.sml").image;
                            break;
                        case MessageType.Warning:
                            nodenameContent.image = EditorGUIUtility.isProSkin ? EditorGUIUtility.IconContent("d_console.warnicon.sml").image : EditorGUIUtility.IconContent("console.warnicon.sml").image;
                            break;
                        default:
                            nodenameContent.image = null;
                            break;
                    }

                    var hasMixedNodeValues = property.hasMultipleDifferentValues;
                    EditorGUI.showMixedValue = hasMixedNodeValues;

                    // Generate menu with node list only when user actually opens it
                    if (EditorGUI.DropdownButton(nodeNameFieldPosition, nodenameContent, FocusType.Keyboard))
                    {
                        var menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Edit..."), false, () =>
                        {
                            editNodeAsText = true;
                            focusNodeTextField = true;
                        });

                        menu.AddSeparator("");

                        menu.AddItem(new GUIContent("<None>"), !nodeNameSet, () =>
                        {
                            property.stringValue = "";
                            property.serializedObject.ApplyModifiedProperties();
                        });

                        if (!referenceExists && nodeNameSet && !hasMixedNodeValues)
                        {
                            menu.AddItem(new GUIContent(nodeName + " (Missing)"), true, () =>
                            {
                                property.stringValue = nodeName;
                                property.serializedObject.ApplyModifiedProperties();
                            });
                        }

                        foreach (var node in GetNodes(project))
                        {
                            var name = node.Name;

                            menu.AddItem(new GUIContent(name), name == nodeName && !hasMixedNodeValues, () =>
                            {
                                property.stringValue = name;
                                property.serializedObject.ApplyModifiedProperties();
                            });
                        }

                        menu.DropDown(nodeNameFieldPosition);
                    }
                }
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        private static IEnumerable<Node> GetNodes(YarnProject project)
        {
            foreach (var node in project.Program.Nodes.Values)
            {
                if (node.Name.StartsWith("$"))
                {
                    // Skip smart variable nodes
                    continue;
                }

                bool isNodeGroup = false;

                foreach (var header in node.Headers)
                {
                    if (header.Key == Node.NodeGroupHeader)
                    {
                        // This node is part of a node group; don't include it
                        isNodeGroup = true;
                        break;
                    }
                }

                if (!isNodeGroup)
                {
                    yield return node;
                }
            }
        }

        private static bool ShouldEndEditing(string controlName)
        {
            if (GUI.GetNameOfFocusedControl() != controlName)
            {
                return false;
            }

            var keyCode = Event.current.keyCode;
            if (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter)
            {
                return false;
            }

            return true;
        }
    }
}
