#if USE_TMP
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
        private SerializedProperty characterNameContainerProperty;

        private SerializedProperty useTypewriterEffectProperty;
        private SerializedProperty onCharacterTypedProperty;
        private SerializedProperty onPauseStartedProperty;
        private SerializedProperty onPauseEndedProperty;

        private SerializedProperty typewriterEffectSpeedProperty;

        private SerializedProperty continueButtonProperty;

        private SerializedProperty autoAdvanceDialogueProperty;
        private SerializedProperty holdDelayProperty;

        private SerializedProperty paletteProperty;

        public void OnEnable()
        {
            canvasGroupProperty = serializedObject.FindProperty(nameof(LineView.canvasGroup));

            useFadeEffectProperty = serializedObject.FindProperty(nameof(LineView.useFadeEffect));
            fadeInTimeProperty = serializedObject.FindProperty(nameof(LineView.fadeInTime));
            fadeOutTimeProperty = serializedObject.FindProperty(nameof(LineView.fadeOutTime));

            lineTextProperty = serializedObject.FindProperty(nameof(LineView.lineText));
            showCharacterNamePropertyInLineViewProperty = serializedObject.FindProperty(nameof(LineView.showCharacterNameInLineView));
            characterNameTextProperty = serializedObject.FindProperty(nameof(LineView.characterNameText));
            characterNameContainerProperty = serializedObject.FindProperty(nameof(LineView.characterNameContainer));

            useTypewriterEffectProperty = serializedObject.FindProperty(nameof(LineView.useTypewriterEffect));
            onCharacterTypedProperty = serializedObject.FindProperty(nameof(LineView.onCharacterTyped));
            onPauseStartedProperty = serializedObject.FindProperty(nameof(LineView.onPauseStarted));
            onPauseEndedProperty = serializedObject.FindProperty(nameof(LineView.onPauseEnded));
            typewriterEffectSpeedProperty = serializedObject.FindProperty(nameof(LineView.typewriterEffectSpeed));

            continueButtonProperty = serializedObject.FindProperty(nameof(LineView.continueButton));

            autoAdvanceDialogueProperty = serializedObject.FindProperty(nameof(LineView.autoAdvance));
            holdDelayProperty = serializedObject.FindProperty(nameof(LineView.holdTime));

            paletteProperty = serializedObject.FindProperty(nameof(LineView.palette));
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
                EditorGUILayout.PropertyField(onPauseStartedProperty);
                EditorGUILayout.PropertyField(onPauseEndedProperty);
                EditorGUILayout.PropertyField(typewriterEffectSpeedProperty);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(characterNameTextProperty);
            EditorGUILayout.PropertyField(characterNameContainerProperty);

            // what to show for character name stuff is a bit weird
            // because the showCharacterNamePropertyInLineView is dependent on two fields there isn't a nice way to just enable it
            byte validNameSetup = 0;
            if (characterNameTextProperty.objectReferenceValue != null)
            {
                validNameSetup += 1;
            }
            if (characterNameContainerProperty.objectReferenceValue != null)
            {
                validNameSetup += 1;
            }
            switch (validNameSetup)
            {
                // neither is configured so we show showCharacterNamePropertyInLineViewProperty
                case 0:
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(showCharacterNamePropertyInLineViewProperty);
                    EditorGUI.indentLevel -= 1;
                    break;
                }
                // one is configured, but not the other, so we show showCharacterNamePropertyInLineViewProperty
                // and also show a warning that this is likely bad
                case 1:
                {
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.HelpBox("Only one of the required name properties is set. This will likely result in unusual behaviours.", MessageType.Warning);
                    EditorGUILayout.PropertyField(showCharacterNamePropertyInLineViewProperty);
                    EditorGUI.indentLevel -= 1;
                    // show a warning in here
                    break;
                }
            }

            EditorGUILayout.PropertyField(continueButtonProperty);

            EditorGUILayout.PropertyField(paletteProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
