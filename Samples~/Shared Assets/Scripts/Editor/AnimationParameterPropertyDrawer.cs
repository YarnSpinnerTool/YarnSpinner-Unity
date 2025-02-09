using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Yarn.Unity.Samples.Editor
{

    [CustomPropertyDrawer(typeof(AnimationParameterAttribute))]
    public class AnimationParameterPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                if (property.propertyType != SerializedPropertyType.String)
                {
                    EditorGUI.HelpBox(position, "Invalid property type " + property.propertyType, MessageType.Error);
                    return;
                }

                var attribute = this.attribute as AnimationParameterAttribute
                    ?? throw new System.InvalidOperationException($"Target is not {nameof(AnimationParameterAttribute)}");
                var animator = property.serializedObject.FindProperty(attribute.AnimatorPropertyName)?.objectReferenceValue as Animator;

                if (animator == null)
                {
                    using (new EditorGUI.DisabledScope())
                    {
                        EditorGUI.Popup(position, label, 0, new GUIContent[] { });
                    }
                    return;
                }

                var parameters = animator.parameters.Where(p => attribute.RequiresSpecificType ? p.type == attribute.Type : true);

                var parameterNames = parameters.Select(p => p.name).ToList();
                var parameterContent = parameterNames.Select(p => new GUIContent(p)).ToArray();

                var selectedIndex = parameterNames.IndexOf(property.stringValue);

                selectedIndex = EditorGUI.Popup(position, label, selectedIndex, parameterContent);

                if (selectedIndex >= 0 && selectedIndex < parameterNames.Count)
                {
                    property.stringValue = parameterNames[selectedIndex];
                }
            }
        }
    }
}