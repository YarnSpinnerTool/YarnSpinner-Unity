/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEditor;
using UnityEngine;

namespace Yarn.Unity
{
    [CustomPropertyDrawer(typeof(DialogueReference))]
    public class DialogueReferenceDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            var projectProperty = property.FindPropertyRelative(nameof(DialogueReference.project));
            var nodeNameProperty = property.FindPropertyRelative(nameof(DialogueReference.nodeName));

            position = GetLineRect(position, out var projectRect);
            position = GetLineRect(position, out var nodeNameRect);
            EditorGUI.PropertyField(projectRect, projectProperty, GUIContent.none);
            EditorGUI.PropertyField(nodeNameRect, nodeNameProperty, GUIContent.none);
        }

        /// <summary>
        /// Given a rectangle, computes a rectangle of the same width as the
        /// input with the height of a single line, taking into account vertical
        /// spacing.
        /// </summary>
        /// <param name="input">A rectangle representing the total area
        /// availalbe.</param>
        /// <param name="lineRect">On return, a rectangle with the same width as
        /// <paramref name="input"/> and with a single line's worth of
        /// height.</param>
        /// <returns>The remaining available space.</returns>
        private Rect GetLineRect(Rect input, out Rect lineRect)
        {
            lineRect = input;
            lineRect.height = EditorGUIUtility.singleLineHeight;

            var remainder = input;
            var offset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            remainder.y += offset;
            remainder.height -= offset;
            return remainder;
        }

        /// <summary>
        /// Returns the height, in pixels, needed for drawing the inspector for
        /// a <see cref="DialogueReference"/>.
        /// </summary>
        /// <param name="property">A serialized property representing a <see
        /// cref="DialogueReference"/> object.</param>
        /// <param name="label">The label to display for <paramref
        /// name="property"/>.</param>
        /// <returns>The displayed height for the property.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int lineCount = 2;
            return lineCount * EditorGUIUtility.singleLineHeight +
                (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
