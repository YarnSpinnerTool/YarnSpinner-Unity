using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public class DialogueReference
    {
        /// <summary>
        /// The Yarn Project asset containing the dialogue node.
        /// </summary>
        public YarnProject project;

        /// <summary>
        /// The name of the dialogue node in the project.
        /// </summary>
        public string nodeName;

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
    }

#if UNITY_EDITOR
    /// <summary>
    /// Property drawer for <see cref="DialogueReference"/>
    /// </summary>
    [CustomPropertyDrawer(typeof(DialogueReference))]
    public class DialogueReferenceDrawer : PropertyDrawer
    {
        private const string NodeTextControlNamePrefix = "DialogueReference.NodeName.";

        private YarnProject lastProject;
        private string lastNodeName;
        private bool referenceExists;
        private bool editNodeAsText;
        private bool focusNodeTextField;

        private GUIContent nodenameContent;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            position = EditorGUI.PrefixLabel(position, controlId, label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // -- Yarn Project asset reference
            var projectProp = property.FindPropertyRelative(nameof(DialogueReference.project));

            YarnProject project = null;
            if (!projectProp.hasMultipleDifferentValues)
            {
                project = (YarnProject)projectProp.objectReferenceValue;
            }

            var projectFieldPosition = position;
            projectFieldPosition.height = EditorGUIUtility.singleLineHeight;

            var nodeNameFieldPosition = position;
            nodeNameFieldPosition.height = EditorGUIUtility.singleLineHeight;
            nodeNameFieldPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(projectFieldPosition, projectProp, GUIContent.none);

            // -- Node name drop down
            var nodeNameProp = property.FindPropertyRelative(nameof(DialogueReference.nodeName));

            // If we want to edit this nodes name as a text field, or if we have
            // multiple values, show a text field and not a dropdown.
            if (editNodeAsText || projectProp.hasMultipleDifferentValues)
            {
                var controlName = NodeTextControlNamePrefix + controlId;

                // Multi-selection with different projects, just show a text
                // field to edit the node name. Most of the time, it will show
                // the mixed value dash (—).
                GUI.SetNextControlName(controlName);
                EditorGUI.PropertyField(nodeNameFieldPosition, nodeNameProp, GUIContent.none);

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
                    var nodeName = nodeNameProp.stringValue;
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

                    var hasMixedNodeValues = nodeNameProp.hasMultipleDifferentValues;
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
                            nodeNameProp.stringValue = "";
                            nodeNameProp.serializedObject.ApplyModifiedProperties();
                        });

                        if (!referenceExists && nodeNameSet && !hasMixedNodeValues)
                        {
                            menu.AddItem(new GUIContent(nodeName + " (Missing)"), true, () =>
                            {
                                nodeNameProp.stringValue = nodeName;
                                nodeNameProp.serializedObject.ApplyModifiedProperties();
                            });
                        }

                        foreach (var name in project.Program.Nodes.Keys)
                        {
                            menu.AddItem(new GUIContent(name), name == nodeName && !hasMixedNodeValues, () =>
                            {
                                nodeNameProp.stringValue = name;
                                nodeNameProp.serializedObject.ApplyModifiedProperties();
                            });
                        }

                        menu.DropDown(nodeNameFieldPosition);
                    }
                }
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineCount = 2;

            return (EditorGUIUtility.singleLineHeight * lineCount)
                 + (EditorGUIUtility.standardVerticalSpacing * (lineCount - 1));
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
#endif
}
