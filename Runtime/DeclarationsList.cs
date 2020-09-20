using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "DeclarationsList", menuName = "Yarn Spinner/Declarations List", order = 0)]
public class DeclarationsList : ScriptableObject {
    [System.Serializable]
    public class Declaration {
        public string name = "$variable";
        public Yarn.Type type = Yarn.Type.String;
        public bool defaultValueBool;
        public float defaultValueNumber;
        public string defaultValueString;

        public string description;
    }

    public List<Declaration> declarations;
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(DeclarationsList.Declaration))]
public class DeclarationPropertyDrawer: PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        Rect RectForFieldIndex(int index, int lineCount = 1) {
            float verticalOffset = EditorGUIUtility.singleLineHeight * index + EditorGUIUtility.standardVerticalSpacing * index;
            float height = EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);

            return new Rect(
                position.x,
                position.y + verticalOffset,
                position.width,
                height
            );
        }

        var foldoutPosition = RectForFieldIndex(0);

        SerializedProperty nameProperty = property.FindPropertyRelative("name");
        string name = nameProperty.stringValue;
        if (string.IsNullOrEmpty(name)) {
            name = "Variable";
        } 
        property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, name);
        
        if (property.isExpanded) {
            var namePosition = RectForFieldIndex(1);
            var typePosition = RectForFieldIndex(2);
            var defaultValuePosition = RectForFieldIndex(3);
            var descriptionPosition = RectForFieldIndex(4, 2);

            EditorGUI.PropertyField(namePosition, nameProperty);
            SerializedProperty typeProperty = property.FindPropertyRelative("type");


            EditorGUI.PropertyField(typePosition, typeProperty);

            SerializedProperty defaultValueProperty;
            
            switch ((Yarn.Type)typeProperty.enumValueIndex)
            {
                case Yarn.Type.Number:
                    defaultValueProperty = property.FindPropertyRelative("defaultValueNumber");
                    break;
                case Yarn.Type.String:
                    defaultValueProperty = property.FindPropertyRelative("defaultValueString");
                    break;
                case Yarn.Type.Bool:
                    defaultValueProperty = property.FindPropertyRelative("defaultValueBool");
                    break;  
                default:
                    defaultValueProperty = null;  
                    break;            
            }

            
            if (defaultValueProperty == null) {
                EditorGUI.LabelField(defaultValuePosition, "Default Value", $"Variable type {(Yarn.Type)typeProperty.enumValueIndex} is not allowed");
            } else {
                EditorGUI.PropertyField(defaultValuePosition, defaultValueProperty,  new GUIContent("Default Value"));
            }
            
            var wordWrappedTextField = EditorStyles.textField;
            wordWrappedTextField.wordWrap = true;
            
            SerializedProperty descriptionProperty = property.FindPropertyRelative("description");
            descriptionProperty.stringValue = EditorGUI.TextField(descriptionPosition, descriptionProperty.displayName, descriptionProperty.stringValue, wordWrappedTextField);
            
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {

        int lines;

        if (property.isExpanded) {
            lines = 6;
        } else {
            lines = 1;
        }

        return base.GetPropertyHeight(property, label) * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
    }


}
#endif
