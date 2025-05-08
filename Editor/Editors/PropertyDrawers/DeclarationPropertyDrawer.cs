/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Linq;
using UnityEditor;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Editor
{
    [CustomPropertyDrawer(typeof(YarnProjectImporter.SerializedDeclaration))]
    public class DeclarationPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws either a property field or a label field for <paramref
        /// name="property"/> at <paramref name="position"/>, depending on
        /// the value of <paramref name="readOnly"/>.
        /// </summary>
        /// <param name="position">The rectangle in which to draw the
        /// control.</param>
        /// <param name="property">The property to draw a control
        /// for.</param>
        /// <param name="readOnly">Whether the property is read-only or
        /// not.</param>
        private void DrawPropertyField(Rect position, SerializedProperty property, bool readOnly, string? label = null)
        {
            if (label == null)
            {
                label = property.displayName;
            }
            if (readOnly)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        EditorGUI.LabelField(position, label, property.intValue.ToString());
                        break;
                    case SerializedPropertyType.Boolean:
                        var boolText = property.boolValue ? "True" : "False";
                        EditorGUI.LabelField(position, label, boolText);
                        break;
                    case SerializedPropertyType.Float:
                        EditorGUI.LabelField(position, label, property.floatValue.ToString());
                        break;
                    case SerializedPropertyType.String:
                        EditorGUI.LabelField(position, label, property.stringValue);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUI.ObjectField(position, property);
                        }
                        break;
                    case SerializedPropertyType.Enum:
                        var displayValue = property.enumDisplayNames[property.enumValueIndex];
                        EditorGUI.LabelField(position, label, displayValue);
                        break;
                }
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = EditorGUI.FloatField(position, label, property.floatValue);
                        break;
                    case SerializedPropertyType.Integer:
                        property.floatValue = EditorGUI.IntField(position, label, property.intValue);
                        break;
                    default:
                        // Just use a regular field for other kinds of
                        // properties
                        EditorGUI.PropertyField(position, property, new GUIContent(label));
                        break;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // A serialized declaration is read-only if it came from a Yarn
            // script. We don't allow editing those in this panel, because
            // the text of the Yarn script belongs to the user.
            bool propertyIsReadOnly = property.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null;

            propertyIsReadOnly |= property.FindPropertyRelative("isImplicit").boolValue;

            const float leftInset = 8;

            Rect RectForFieldIndex(int index, int lineCount = 1)
            {
                float verticalOffset = EditorGUIUtility.singleLineHeight * index + EditorGUIUtility.standardVerticalSpacing * index;
                float height = EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);

                return new Rect(
                    position.x + leftInset,
                    position.y + verticalOffset,
                    position.width - leftInset,
                    height
                );
            }

            var foldoutPosition = RectForFieldIndex(0);

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            string name = nameProperty.stringValue;
            if (string.IsNullOrEmpty(name))
            {
                name = "Variable";
            }

            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, name);

            if (property.isExpanded)
            {
                var namePosition = RectForFieldIndex(1);
                var typePosition = RectForFieldIndex(2);
                var defaultValuePosition = RectForFieldIndex(3);
                var descriptionPosition = RectForFieldIndex(4, 2);
                var sourcePosition = RectForFieldIndex(6);

                DrawPropertyField(namePosition, nameProperty, propertyIsReadOnly);

                SerializedProperty typeProperty = property.FindPropertyRelative("typeName");

                if (propertyIsReadOnly)
                {
                    DrawPropertyField(typePosition, typeProperty, true);
                }
                else
                {
                    var popupElements = YarnProjectImporter.SerializedDeclaration.BuiltInTypesList;
                    var popupElementNames = popupElements.Select(t => t.Name).ToList();
                    var selectedIndex = popupElementNames.IndexOf(typeProperty.stringValue);

                    var prefixPosition = EditorGUI.PrefixLabel(typePosition, new GUIContent("Type"));

                    selectedIndex = EditorGUI.Popup(prefixPosition, selectedIndex, popupElementNames.ToArray());
                    if (selectedIndex >= 0 && selectedIndex <= popupElementNames.Count)
                    {
                        typeProperty.stringValue = popupElementNames[selectedIndex];
                    }
                }

                SerializedProperty? defaultValueProperty;

                var type = YarnProjectImporter.SerializedDeclaration.BuiltInTypesList.FirstOrDefault(t => t.Name == typeProperty.stringValue);

                if (type == Types.Number)
                {
                    defaultValueProperty = property.FindPropertyRelative("defaultValueNumber");
                }
                else if (type == Types.String)
                {
                    defaultValueProperty = property.FindPropertyRelative("defaultValueString");
                }
                else if (type == Types.Boolean)
                {
                    defaultValueProperty = property.FindPropertyRelative("defaultValueBool");
                }
                else
                {
                    defaultValueProperty = null;
                }


                if (defaultValueProperty == null)
                {
                    EditorGUI.LabelField(defaultValuePosition, "Default Value", $"Variable type {typeProperty.stringValue} is not allowed");
                }
                else
                {
                    DrawPropertyField(defaultValuePosition, defaultValueProperty, propertyIsReadOnly, "Default Value");
                }


                // Don't use DrawPropertyField here because we want to use
                // a special gui style and directly use the string value
                SerializedProperty descriptionProperty = property.FindPropertyRelative("description");
                if (propertyIsReadOnly)
                {
                    descriptionPosition = EditorGUI.PrefixLabel(descriptionPosition, new GUIContent(descriptionProperty.displayName));
                    EditorGUI.SelectableLabel(descriptionPosition, descriptionProperty.stringValue, EditorStyles.wordWrappedLabel);
                }
                else
                {
                    var wordWrappedTextField = EditorStyles.textField;
                    wordWrappedTextField.wordWrap = true;

                    descriptionProperty.stringValue = EditorGUI.TextField(descriptionPosition, descriptionProperty.displayName, descriptionProperty.stringValue, wordWrappedTextField);
                }

                if (!propertyIsReadOnly)
                {
                    EditorGUI.LabelField(sourcePosition, "Declared In", "this file");
                }
                else
                {
                    SerializedProperty sourceProperty = property.FindPropertyRelative("sourceYarnAsset");
                    EditorGUI.ObjectField(sourcePosition, "Declared In", sourceProperty.objectReferenceValue, typeof(TextAsset), false);
                }


            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            return GetPropertyHeightImpl(property, label);
        }

        public static float GetPropertyHeightImpl(SerializedProperty property, GUIContent label)
        {
            int lines;

            if (property.isExpanded)
            {
                lines = 7;
            }
            else
            {
                lines = 1;
            }

            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * lines + 1;
        }
    }
}
