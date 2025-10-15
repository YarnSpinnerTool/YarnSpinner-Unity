#nullable enable

namespace Yarn.Unity.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(MarkupPalette.BasicMarker))]
    public class BasicMarkerPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var name = property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Marker));

            var nameField = new PropertyField(name);
            var showColourField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.CustomColor)));
            var colourField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Color)));
            var boldField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Boldened)));
            var italicsField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Italicised)));
            var underlinedField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Underlined)));
            var strikedField = new PropertyField(property.FindPropertyRelative(nameof(MarkupPalette.BasicMarker.Strikedthrough)));

            var foldout = new Foldout { text = name.stringValue };

            showColourField.RegisterCallback((ChangeEvent<bool> b) =>
            {
                colourField.style.display = b.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            
            foldout.Add(nameField);
            foldout.Add(showColourField);
            foldout.Add(colourField);
            foldout.Add(boldField);
            foldout.Add(italicsField);
            foldout.Add(underlinedField);
            foldout.Add(strikedField);

            container.Add(foldout);

            return container;
        }
    }
}