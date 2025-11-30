using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{
    [CustomPropertyDrawer(typeof(InterfaceContainer<>), true)]
    public class InterfaceContainerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProp = property.FindPropertyRelative(nameof(InterfaceContainer<UnityEngine.Object>.targetObject));
            EditorGUI.ObjectField(position, targetProp, new UnityEngine.GUIContent(property.displayName));
        }
    }
}
