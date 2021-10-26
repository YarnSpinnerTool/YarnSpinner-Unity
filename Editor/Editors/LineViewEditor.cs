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

        private SerializedProperty continueActionTypeProperty;
        private SerializedProperty continueActionKeyCodeProperty;
        private SerializedProperty continueActionReferenceProperty;
        private SerializedProperty continueActionProperty;

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
            continueActionTypeProperty = serializedObject.FindProperty(nameof(LineView.continueActionType));
            continueActionKeyCodeProperty = serializedObject.FindProperty(nameof(LineView.continueActionKeyCode));

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            continueActionReferenceProperty = serializedObject.FindProperty(nameof(LineView.continueActionReference));
            continueActionProperty = serializedObject.FindProperty(nameof(LineView.continueAction));
#endif

        }

        public override void OnInspectorGUI()
        {
            var baseProperties = new[] {
                canvasGroupProperty,

                lineTextProperty,

            };
            foreach (var prop in baseProperties)
            {
                EditorGUILayout.PropertyField(prop);
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

            EditorGUILayout.PropertyField(continueActionTypeProperty);

            switch ((LineView.ContinueActionType)continueActionTypeProperty.enumValueIndex)
            {
                case LineView.ContinueActionType.None:
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.HelpBox($"After each line has finished appearing, this line view will stop and wait.\n\nTo continue to the next line, you will need to call {nameof(LineView.OnContinueClicked)} on this component, or turn on the {ObjectNames.NicifyVariableName(nameof(DialogueRunner))}'s \"{ObjectNames.NicifyVariableName(nameof(DialogueRunner.automaticallyContinueLines))}\" setting.", MessageType.Info);
                    EditorGUI.indentLevel -= 1;
                    break;
                case LineView.ContinueActionType.KeyCode:
                    EditorGUILayout.PropertyField(continueActionKeyCodeProperty);
                    break;
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                case LineView.ContinueActionType.InputSystemAction:
                    EditorGUILayout.PropertyField(continueActionProperty);
                    break;
                case LineView.ContinueActionType.InputSystemActionFromAsset:
                    EditorGUILayout.PropertyField(continueActionReferenceProperty);
                    break;
#else
                default:
                    EditorGUILayout.HelpBox("Please install and enable the Unity Input System.", MessageType.Warning);
                    break;
#endif
            }

            EditorGUILayout.PropertyField(continueButtonProperty);

            serializedObject.ApplyModifiedProperties();

        }
    }

}
