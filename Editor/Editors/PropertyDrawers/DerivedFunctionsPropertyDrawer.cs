/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{
    [CustomPropertyDrawer(typeof(FunctionInfo))]
    public class DerivedFunctionsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

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

            SerializedProperty nameProperty = property.FindPropertyRelative("Name");
            string name = nameProperty?.stringValue ?? "FUNCTION NAME";

            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, name);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel += 1;
                var typePosition = RectForFieldIndex(1);
                var paramPosition = RectForFieldIndex(2);

                SerializedProperty typeProperty = property.FindPropertyRelative("ReturnType");
                EditorGUI.LabelField(typePosition, typeProperty?.stringValue ?? "RETURN");

                SerializedProperty paramProperty = property.FindPropertyRelative("Parameters");
                int count = paramProperty?.arraySize ?? 0;
                if (count > 0)
                {
                    string[] p = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        p[i] = paramProperty.GetArrayElementAtIndex(i).stringValue;
                    }
                    EditorGUI.LabelField(paramPosition, $"({string.Join(", ", p)})");
                }
                else
                {
                    EditorGUI.LabelField(paramPosition, $"No Parameters");
                }
                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            int lines = 1;

            if (property.isExpanded)
            {
                lines = 3;
            }

            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * lines + 1;
        }
    }
}
