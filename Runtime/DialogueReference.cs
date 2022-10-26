using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// Reference to a dialogue node in a Yarn Project.
    /// </summary>
    /// <remarks>
    /// A combination of Yarn Project asset reference and dialog name,
    /// allowing to check if the node exists in the project.
    /// Comes with a property drawer that allows to select the node
    /// from a drop-down and warns if the node doesn't exist.
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
        /// Create an empty dialogue reference.
        /// </summary>
        public DialogueReference() { }

        /// <summary>
        /// Create a dialogue reference with a given project and node name.
        /// </summary>
        /// <param name="project">Yarn Project asset containing the node.</param>
        /// <param name="nodeName">Name of the node in the project asset.</param>
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
        const string NodeTextControlNamePrefix = "DialogueReference.NodeName.";

        YarnProject lastProject;
        string lastNodeName;
        bool referenceExists;
        bool editNodeAsText;
        bool focusNodeTextField;

        GUIContent nodenameContent;

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

            var showNodeDropdown = (projectProp.hasMultipleDifferentValues || project != null);
            if (showNodeDropdown)
            {
                position.width /= 2f;
            }

            EditorGUI.PropertyField(position, projectProp, GUIContent.none);

            if (showNodeDropdown)
            {
                position.x += position.width;
            }

            // -- Node name drop down
            var nodeNameProp = property.FindPropertyRelative(nameof(DialogueReference.nodeName));
            if (editNodeAsText || projectProp.hasMultipleDifferentValues)
            {
                var controlName = NodeTextControlNamePrefix + controlId;

                // Multi-selection with different projects,
                // just show a text field to edit the node name.
                // Most of the time, it will show the mixed value dash (—).
                GUI.SetNextControlName(controlName);
                EditorGUI.PropertyField(position, nodeNameProp, GUIContent.none);

                if (editNodeAsText)
                {
                    if (focusNodeTextField)
                    {
                        // Focusing the text field is delayed like this because
                        // the control needs to exist first before we can focus it
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
            else if (showNodeDropdown)
            {
                var nodeName = nodeNameProp.stringValue;
                var nodeNameSet = !string.IsNullOrEmpty(nodeName);

                // Cached check if node exists in project
                if (lastProject != project || lastNodeName != nodeName)
                {
                    lastProject = project;
                    lastNodeName = nodeName;
                    referenceExists = project.GetProgram().Nodes.ContainsKey(nodeName);
                }

                if (nodenameContent == null)
                {
                    nodenameContent = new GUIContent();
                }

                // Show warning icon if not does not exist in selected project
                nodenameContent.text = nodeName;
                if (referenceExists || !nodeNameSet)
                {
                    nodenameContent.image = null;
                }
                else if (nodenameContent.image == null)
                {
                    nodenameContent.image = EditorGUIUtility.IconContent("d_console.warnicon.sml").image;
                }

                var hasMixedNodeValues = nodeNameProp.hasMultipleDifferentValues;
                EditorGUI.showMixedValue = hasMixedNodeValues;

                // Generate menu with node list only when user actually opens it
                if (EditorGUI.DropdownButton(position, nodenameContent, FocusType.Keyboard))
                {
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Edit..."), false, () => {
                        editNodeAsText = true;
                        focusNodeTextField = true;
                    });

                    GenericMenu.MenuFunction copyAction = null;
                    if (nodeNameSet && !hasMixedNodeValues)
                    {
                        copyAction = () => {
                            EditorGUIUtility.systemCopyBuffer = nodeName;
                        };
                    }
                    menu.AddItem(new GUIContent("Copy"), false, copyAction);

                    GenericMenu.MenuFunction pasteAction = null;
                    var pasteNode = EditorGUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(pasteNode))
                    {
                        pasteAction = () => {
                            nodeNameProp.stringValue = pasteNode;
                            nodeNameProp.serializedObject.ApplyModifiedProperties();
                        };
                    }
                    menu.AddItem(new GUIContent("Paste"), false, pasteAction);

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("<None>"), !nodeNameSet, () => {
                        nodeNameProp.stringValue = "";
                        nodeNameProp.serializedObject.ApplyModifiedProperties();
                    });

                    if (!referenceExists && nodeNameSet && !hasMixedNodeValues)
                    {
                        menu.AddItem(new GUIContent(nodeName + " (Missing)"), true, () => { 
                            nodeNameProp.stringValue = nodeName;
                            nodeNameProp.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    foreach (var name in project.GetProgram().Nodes.Keys)
                    {
                        menu.AddItem(new GUIContent(name), (name == nodeName && !hasMixedNodeValues), () => { 
                            nodeNameProp.stringValue = name;
                            nodeNameProp.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.DropDown(position);
                }
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        static bool ShouldEndEditing(string controlName)
        {
            if (GUI.GetNameOfFocusedControl() != controlName)
                return false;

            var keyCode = Event.current.keyCode;
            if (keyCode != KeyCode.Return && keyCode != KeyCode.KeypadEnter)
                return false;

            return true;
        }
    }
#endif
}
