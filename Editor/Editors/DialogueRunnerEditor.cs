/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEditor;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using Yarn.Unity.UnityLocalization;

namespace Yarn.Unity.Editor
{

#nullable enable

    delegate void PropertyRenderer(SerializedProperty property);


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CustomUIForAttribute : YarnEditorAttribute
    {
        public string propertyName;

        public CustomUIForAttribute(string methodName)
        {
            this.propertyName = methodName;
        }
    }

    internal static class AttributeExtensions
    {
        public static bool Evaluate(this VisibilityAttribute visibilityAttribute, SerializedObject target)
        {
            var property = target.FindProperty(visibilityAttribute.condition);

            if (property == null)
            {
                // Property is missing; default to showing the field
                return true;
            }

            bool result;

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    result = property.objectReferenceValue != null;
                    break;
                case SerializedPropertyType.Boolean:
                    result = property.boolValue;
                    break;
                default:
                    // Property is an unhandled type; default to showing the field
                    return true;
            }

            if (visibilityAttribute.invert)
            {
                result = !result;
            }
            return result;
        }
    }

    struct PropertyInfo
    {
        public readonly SerializedProperty serializedProperty;
        public readonly YarnEditorAttribute[] attributes;
        private readonly FieldInfo? field;

        public static IEnumerable<Attribute> GetAttributes(SerializedProperty property)
        {
            FieldInfo? field = GetField(property);
            if (field != null)
            {
                return field.GetCustomAttributes();
            }
            else
            {
                Debug.LogWarning($"Failed to find field {property.name} on object {property.serializedObject.targetObject.name}");
                return Array.Empty<Attribute>();
            }
        }

        private static FieldInfo? GetField(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            var field = target.GetType().GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field;
        }

        public PropertyInfo(SerializedProperty property)
        {
            this.serializedProperty = property;
            this.attributes = GetAttributes(property).OfType<YarnEditorAttribute>().ToArray();
            this.field = GetField(property);
        }

        public bool IsInherited {
            get {
                if (this.field == null) {
                    return false;
                }
                var owner = this.serializedProperty.serializedObject.targetObject;
                var ownerType = owner.GetType();
                var declaringType = this.field.DeclaringType;
                return ownerType == declaringType;
            }
        }
    }

    public abstract class YarnEditor : UnityEditor.Editor {

#if USE_UNITY_LOCALIZATION
        protected const bool UnityLocalizationAvailable = true;
#else
        protected const bool UnityLocalizationAvailable = false;
#endif

        internal const string ScriptPropertyName = "m_Script";
        private static bool ShowCallbacks = false;

        private string? currentGroup;

        private Dictionary<string, PropertyInfo> propertyInfos = new Dictionary<string, PropertyInfo>();

        private Dictionary<string, PropertyRenderer> customPropertyRenderers = new Dictionary<string, PropertyRenderer>();

        private void DrawPropertyField(PropertyInfo property)
        {

            int indentation = 0;
            GroupAttribute? group = null;
            string? label = null;

            // Get all relevant attributes for this property and get information
            // from it.
            foreach (var attr in property.attributes)
            {
                switch (attr)
                {
                    case VisibilityAttribute visibilityAttribute:
                        if (visibilityAttribute.Evaluate(property.serializedProperty.serializedObject) == false)
                        {
                            // A visibility attribute has indicated that we shouldn't
                            // show the field, so skip it
                            return;
                        }
                        break;
                    case IndentAttribute indentAttribute:
                        indentation = indentAttribute.indentLevel;
                        break;
                    case GroupAttribute groupAttribute:
                        group = groupAttribute;
                        break;
                    case LabelAttribute labelAttribute:
                        label = labelAttribute.label;
                        break;
                }
            }

            // Gets a unique string ID for a given group on a specific object.
            string GetGroupID(GroupAttribute group)
            {
                var target = property.serializedProperty.serializedObject.targetObject;
                var uniqueGroupID = $"{target.GetType()}_{target.GetInstanceID()}_group_{group.groupName}";
                return uniqueGroupID;
            }

            // Renders the header of a group. If the group is a foldout, renders
            // the header and manages its state.
            void StartGroup(GroupAttribute group)
            {
                if (group.foldOut)
                {
                    var uniqueGroupID = GetGroupID(group);

                    var isToggled = SessionState.GetBool(uniqueGroupID, false);
                    GUIContent content = new GUIContent(group.groupName);
                    isToggled = EditorGUILayout.Foldout(isToggled, content, EditorStyles.foldoutHeader);
                    SessionState.SetBool(uniqueGroupID, isToggled);
                }
                else
                {
                    EditorGUILayout.LabelField(group.groupName, EditorStyles.boldLabel);
                }
                EditorGUI.indentLevel += 1;
            }

            void EndGroup()
            {
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }

            // Figure out if we're starting a new group, leaving the group, or
            // switching to a new group
            if (currentGroup == null && group != null)
            {
                // We've started a group.
                StartGroup(group);
            }
            else if (currentGroup != null && group == null)
            {
                // We've left the current group.
                EndGroup();
            }
            else if (currentGroup != null && group != null && currentGroup.Equals(group.groupName, StringComparison.Ordinal) == false)
            {
                // We've changed group.
                EndGroup();
                StartGroup(group);
            }

            currentGroup = group?.groupName;

            if (group?.foldOut ?? false)
            {
                var id = GetGroupID(group);
                var isOpen = SessionState.GetBool(id, false);
                if (!isOpen)
                {
                    // We're in a group that's not open. Don't render this
                    // property.
                    return;
                }
            }


            EditorGUI.indentLevel += indentation;

            if (customPropertyRenderers.TryGetValue(property.serializedProperty.name, out var customRenderer))
            {
                // We have a custom renderer for this property - use it!
                customRenderer.Invoke(property.serializedProperty);
            }
            else
            {
                // Use the default renderer for this property
                if (label == null)
                {
                    EditorGUILayout.PropertyField(property.serializedProperty);
                }
                else
                {
                    EditorGUILayout.PropertyField(property.serializedProperty, new GUIContent(label));
                }
            }

            EditorGUI.indentLevel -= indentation;
        }

        private void DrawPropertyField(SerializedProperty property)
        {
            if (propertyInfos.TryGetValue(property.name, out var propertyInfo))
            {
                DrawPropertyField(propertyInfo);
            }
            else
            {
                // We don't have a PropertyInfo for this property - just draw the default field
                EditorGUILayout.PropertyField(property);
            }
        }

        private void OnEnable()
        {
            propertyInfos.Clear();
            customPropertyRenderers.Clear();

            // Build a dictionary of property names to SerializedProperties
            Dictionary<string, SerializedProperty> namesToProperties = new Dictionary<string, SerializedProperty>();

            // Find all properties on the target, and build a PropertyInfo
            // struct for it that will contain all of its relevant attributes.
            var propertyIterator = serializedObject.GetIterator();
            propertyIterator.NextVisible(true);
            do
            {
                var propertyInfo = new PropertyInfo(propertyIterator.Copy());
                propertyInfos.Add(propertyIterator.name, propertyInfo);

                namesToProperties[propertyIterator.name] = propertyIterator.Copy();
            } while (propertyIterator.NextVisible(false));

            // Find all methods on this object that have a CustomUI attribute,
            // and remember that that method should be called for handling the
            // relevant property.
            var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CustomUIForAttribute>();
                if (attr == null)
                {
                    // No CustomUI attribute on this method. Move on.
                    continue;
                }

                // Does the attribute reference a property that exists?
                if (propertyInfos.TryGetValue(attr.propertyName, out var prop) == false)
                {
                    // We don't have a property named attr.propertyName. Log a warning about it.
                    Debug.LogWarning($"{serializedObject.targetObject.GetType()} has no property '{attr.propertyName}' (or it is not visible)");
                    continue;
                }

                // Does the attribute reference a property that we already have a renderer for?
                if (this.customPropertyRenderers.ContainsKey(attr.propertyName))
                {
                    Debug.LogWarning($"{nameof(DialogueRunnerEditor)} already has a custom renderer for {attr.propertyName}");
                    continue;
                }

                PropertyRenderer propertyRenderer = (PropertyRenderer)method.CreateDelegate(typeof(PropertyRenderer), this);
                customPropertyRenderers.Add(attr.propertyName, propertyRenderer);
                Debug.Log($"Registered custom drawer {method.Name} for property {attr.propertyName}");
            }
        }

        public override void OnInspectorGUI()
        {
            this.currentGroup = null;

            // Start iterating the list of properties
            var currentProperty = serializedObject.GetIterator();
            currentProperty.NextVisible(true);

            do
            {
                if (currentProperty.name == ScriptPropertyName)
                {
                    // Don't draw the 'script' property
                    continue;
                }

                DrawPropertyField(currentProperty);
            } while (currentProperty.NextVisible(false));

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(VoiceOverView))]
    public class VoiceOverViewEditor : YarnEditor { }

    [CustomEditor(typeof(DialogueRunner))]
    public class DialogueRunnerEditor : YarnEditor
    {
        [CustomUIFor(nameof(DialogueRunner.variableStorage))]
        public void DrawVariableStorage(SerializedProperty variableStorageProperty)
        {
            // Draw the property. If it's null, show an info box.
            EditorGUILayout.PropertyField(variableStorageProperty);
            if (variableStorageProperty.objectReferenceValue == null)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox($"An {ObjectNames.NicifyVariableName(nameof(InMemoryVariableStorage))} component will be added at run time.", MessageType.Info);
                EditorGUI.indentLevel -= 1;
            }
        }

        [CustomUIFor(nameof(DialogueRunner.lineProvider))]
        public void DrawLineProvider(SerializedProperty lineProviderProperty)
        {
            // Draw the line provider.
            EditorGUILayout.PropertyField(lineProviderProperty);

            // Check to see if the line provider is using Unity Localization. If
            // it is, offer some tools to help set it up, if needed.
            var yarnProjectProperty = lineProviderProperty.serializedObject.FindProperty(nameof(DialogueRunner.yarnProject));
            var yarnProject = yarnProjectProperty.objectReferenceValue as YarnProject;
            var yarnProjectIsUnityLoc = yarnProject != null && yarnProject.localizationType == LocalizationType.Unity;

            if (lineProviderProperty.objectReferenceValue == null)
            {
                // We don't have a line provider.
                EditorGUI.indentLevel += 1;
                if (yarnProjectIsUnityLoc)
                {
#if USE_UNITY_LOCALIZATION
                    // If this is a project using Unity localisation, we can't add a
                    // line provider at runtime because we won't know what string
                    // table it should use. In this situation, we'll show a warning
                    // and offer a quick button they can click to add one.
                    string unityLocalizedLineProvider = ObjectNames.NicifyVariableName(nameof(UnityLocalization.UnityLocalisedLineProvider));
                    EditorGUILayout.HelpBox($"This project uses Unity Localization. You will need to add a {unityLocalizedLineProvider} for it to work. Click the button below to add one, and then set it up.", MessageType.Warning);

                    GameObject gameObject = (serializedObject.targetObject as DialogueRunner)!.gameObject;

                    UnityLocalization.UnityLocalisedLineProvider existingLineProvider = gameObject.GetComponent<UnityLocalization.UnityLocalisedLineProvider>();

                    if (existingLineProvider != null)
                    {
                        if (GUILayout.Button($"Use {unityLocalizedLineProvider}"))
                        {
                            lineProviderProperty.objectReferenceValue = existingLineProvider;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button($"Add {unityLocalizedLineProvider}"))
                        {
                            var lineProvider = gameObject.AddComponent<UnityLocalization.UnityLocalisedLineProvider>();

                            lineProviderProperty.objectReferenceValue = lineProvider;
                        }
                    }

#else
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but Unity Localization is not installed. Please install it, or change this Yarn Project to use Yarn Spinner's internal localisation system.", MessageType.Error);
#endif

                }
                else
                {
                    // Otherwise, we'll assume they're using the built-in
                    // localisation system, and we can safely create one at
                    // runtime because we know everything we need to to set that
                    // up.
                    EditorGUILayout.HelpBox($"A {ObjectNames.NicifyVariableName(nameof(BuiltinLocalisedLineProvider))} component will be added at run time.", MessageType.Info);
                }
                EditorGUI.indentLevel -= 1;
            } else {
                // We do have a line provider.

                // If it's a BuiltInLocalisationLineProvider and the project is
                // using Unity localization, that's probably a mistake, and we
                // should warn the user.
                Type lineProviderType = lineProviderProperty.objectReferenceValue.GetType();
                bool lineProviderIsUnityLineProvider = typeof(UnityLocalisedLineProvider).IsAssignableFrom(lineProviderType);

                if (yarnProjectIsUnityLoc && lineProviderIsUnityLineProvider == false)
                {
#if USE_UNITY_LOCALIZATION
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but you are using a {ObjectNames.NicifyVariableName(lineProviderType.Name)}. You should use a {ObjectNames.NicifyVariableName(nameof(UnityLocalisedLineProvider))} instead.", MessageType.Warning);
#else
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but Unity Localization is not installed. Please install it, or change this Yarn Project to use Yarn Spinner's internal localisation system.", MessageType.Error);
#endif
                }
            }
        }


    }
}
