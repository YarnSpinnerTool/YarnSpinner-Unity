using UnityEngine;
using UnityEditor;

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(LineView))]
    public class LineViewEditor : UnityEditor.Editor
    {
        private SerializedProperty canvasGroupProperty;
        private SerializedProperty useFadeEffectProperty;
        private SerializedProperty fadeInTimeProperty;
        private SerializedProperty fadeOutTimeProperty;
        private SerializedProperty lineTextProperty;
        private SerializedProperty showCharacterNamePropertyInLineViewProperty;
        private SerializedProperty characterNameTextProperty;
        private SerializedProperty useTypewriterEffectProperty;
        private SerializedProperty onCharacterTypedProperty;
        private SerializedProperty typewriterEffectSpeedProperty;

        private SerializedProperty continueButtonProperty;

        private SerializedProperty autoAdvanceDialogueProperty;
        private SerializedProperty holdDelayProperty;

        public void OnEnable()
        {

            canvasGroupProperty = serializedObject.FindProperty(nameof(LineView.canvasGroup));
            useFadeEffectProperty = serializedObject.FindProperty(nameof(LineView.useFadeEffect));
            fadeInTimeProperty = serializedObject.FindProperty(nameof(LineView.fadeInTime));
            fadeOutTimeProperty = serializedObject.FindProperty(nameof(LineView.fadeOutTime));
            lineTextProperty = serializedObject.FindProperty(nameof(LineView.lineText));
            showCharacterNamePropertyInLineViewProperty = serializedObject.FindProperty(nameof(LineView.showCharacterNameInLineView));
            characterNameTextProperty = serializedObject.FindProperty(nameof(LineView.characterNameText));
            useTypewriterEffectProperty = serializedObject.FindProperty(nameof(LineView.useTypewriterEffect));
            onCharacterTypedProperty = serializedObject.FindProperty(nameof(LineView.onCharacterTyped));
            typewriterEffectSpeedProperty = serializedObject.FindProperty(nameof(LineView.typewriterEffectSpeed));
            continueButtonProperty = serializedObject.FindProperty(nameof(LineView.continueButton));
            autoAdvanceDialogueProperty = serializedObject.FindProperty(nameof(LineView.autoAdvance));
            holdDelayProperty = serializedObject.FindProperty(nameof(LineView.holdTime));
        }

        public override void OnInspectorGUI()
        {
            var baseProperties = new[] {
                canvasGroupProperty,

                lineTextProperty,

                autoAdvanceDialogueProperty,
            };
            foreach (var prop in baseProperties)
            {
                EditorGUILayout.PropertyField(prop);
            }

            if (autoAdvanceDialogueProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(holdDelayProperty);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(useFadeEffectProperty);

            if (useFadeEffectProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(fadeInTimeProperty);
                EditorGUILayout.PropertyField(fadeOutTimeProperty);
                EditorGUI.indentLevel -= 1;
            }


            EditorGUILayout.PropertyField(useTypewriterEffectProperty);

            if (useTypewriterEffectProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(onCharacterTypedProperty);
                EditorGUILayout.PropertyField(typewriterEffectSpeedProperty);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(characterNameTextProperty);

            if (characterNameTextProperty.objectReferenceValue == null)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(showCharacterNamePropertyInLineViewProperty);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(continueButtonProperty);

            serializedObject.ApplyModifiedProperties();

        }
    }

}
