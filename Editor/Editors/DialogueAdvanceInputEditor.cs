using UnityEngine;
using UnityEditor;
using System;

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(DialogueAdvanceInput))]
    public class DialogueAdvanceInputEditor : UnityEditor.Editor
    {
        // {0} = the name of the target dialogue view, or
        // DialogueViewPlaceholderName if null; {1} = the name of the method to
        // call to advance the dialogue
        private const string InputTypeNoneMessage = "This component will not notify {0} of any input. If you want to signal that the user wants to advance to the next line, you will need to call {1}() yourself.";

        private const string DialogueViewPlaceholderName = "the dialogue view";

        // Disable warning "constant is unused" - these constant are only unused
        // depending on whether the input system is available or not
#pragma warning disable RCS1213 
        private const string InputSystemNotAvailableWarning = "Install and enable the Unity Input System package to use this mode.";
        private const string LegacyInputSystemNotAvailableWarning = "Enable the legacy Input Manager to use this mode.";
#pragma warning restore RCS1213

        private SerializedProperty dialogueViewProperty;
        private SerializedProperty continueActionTypeProperty;
        private SerializedProperty continueActionKeyCodeProperty;
        private SerializedProperty continueActionReferenceProperty;
        private SerializedProperty continueActionProperty;
        private SerializedProperty enableActionOnStartProperty;

        public void OnEnable()
        {
            dialogueViewProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.dialogueView));
            continueActionTypeProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.continueActionType));
            continueActionKeyCodeProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.continueActionKeyCode));

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            continueActionReferenceProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.continueActionReference));
            continueActionProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.continueAction));
            enableActionOnStartProperty = serializedObject.FindProperty(nameof(DialogueAdvanceInput.enableActionOnStart));
#endif
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(dialogueViewProperty);
            EditorGUILayout.PropertyField(continueActionTypeProperty);


            switch (continueActionTypeProperty.enumValueIndex)
            {
                case (int)DialogueAdvanceInput.ContinueActionType.None:
                    DrawInputActionTypeNone();
                    break;

                case (int)DialogueAdvanceInput.ContinueActionType.KeyCode:
                    DrawInputActionTypeKeycode();
                    break;

                case (int)DialogueAdvanceInput.ContinueActionType.InputSystemAction:
                    DrawInputActionTypeAction();
                    break;

                case (int)DialogueAdvanceInput.ContinueActionType.InputSystemActionFromAsset:
                    DrawInputActionTypeActionFromAsset();
                    break;
            }


            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInputActionTypeActionFromAsset()
        {
            EditorGUI.indentLevel += 1;
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            EditorGUILayout.PropertyField(continueActionReferenceProperty);
            EditorGUILayout.PropertyField(enableActionOnStartProperty);
#else
            EditorGUILayout.HelpBox(InputSystemNotAvailableWarning, MessageType.Warning);
#endif
            EditorGUI.indentLevel -= 1;
        }

        private void DrawInputActionTypeAction()
        {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            EditorGUILayout.PropertyField(continueActionProperty);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.PropertyField(enableActionOnStartProperty);
#else
            EditorGUI.indentLevel += 1;
            EditorGUILayout.HelpBox(InputSystemNotAvailableWarning, MessageType.Warning);
#endif
            EditorGUI.indentLevel -= 1;
        }

        private void DrawInputActionTypeKeycode()
        {
            EditorGUI.indentLevel += 1;
#if ENABLE_LEGACY_INPUT_MANAGER
            EditorGUILayout.PropertyField(continueActionKeyCodeProperty);
#else
            EditorGUILayout.HelpBox(LegacyInputSystemNotAvailableWarning, MessageType.Warning);
#endif
            EditorGUI.indentLevel -= 1;

        }

        private void DrawInputActionTypeNone()
        {
            EditorGUI.indentLevel += 1;
            string name;
            UnityEngine.Object objectReferenceValue = dialogueViewProperty.objectReferenceValue;
            if (objectReferenceValue != null)
            {
                name = objectReferenceValue.name;
            }
            else
            {
                name = DialogueViewPlaceholderName;
            }
            EditorGUILayout.HelpBox(string.Format(InputTypeNoneMessage, name, nameof(DialogueViewBase.UserRequestedViewAdvancement)), MessageType.Info);
            EditorGUI.indentLevel -= 1;
        }
    }
}
