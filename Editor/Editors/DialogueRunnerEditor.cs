using UnityEngine;
using UnityEditor;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

using Yarn.Unity;
using System.Linq;
using System.Collections.Generic;

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(DialogueRunner))]
    public class DialogueRunnerEditor : UnityEditor.Editor
    {
        private static bool ShowCallbacks = false;

        private SerializedProperty yarnProjectProperty;
        private SerializedProperty variableStorageProperty;
        private SerializedProperty dialogueViewsProperty;
        private SerializedProperty startNodeProperty;
        private SerializedProperty startAutomaticallyProperty;
        private SerializedProperty runSelectedOptionAsLineProperty;
        private SerializedProperty lineProviderProperty;
        private SerializedProperty verboseLoggingProperty;
        private SerializedProperty onNodeStartProperty;
        private SerializedProperty onNodeCompleteProperty;
        private SerializedProperty onDialogueStartProperty;
        private SerializedProperty onDialogueCompleteProperty;
        private SerializedProperty onCommandProperty;

        private void OnEnable()
        {
            yarnProjectProperty = serializedObject.FindProperty(nameof(DialogueRunner.yarnProject));
            variableStorageProperty = serializedObject.FindProperty(nameof(DialogueRunner._variableStorage));
            dialogueViewsProperty = serializedObject.FindProperty(nameof(DialogueRunner.dialogueViews));
            startNodeProperty = serializedObject.FindProperty(nameof(DialogueRunner.startNode));
            startAutomaticallyProperty = serializedObject.FindProperty(nameof(DialogueRunner.startAutomatically));
            runSelectedOptionAsLineProperty = serializedObject.FindProperty(nameof(DialogueRunner.runSelectedOptionAsLine));
            lineProviderProperty = serializedObject.FindProperty(nameof(DialogueRunner.lineProvider));
            verboseLoggingProperty = serializedObject.FindProperty(nameof(DialogueRunner.verboseLogging));
            onNodeStartProperty = serializedObject.FindProperty(nameof(DialogueRunner.onNodeStart));
            onNodeCompleteProperty = serializedObject.FindProperty(nameof(DialogueRunner.onNodeComplete));
            onDialogueStartProperty = serializedObject.FindProperty(nameof(DialogueRunner.onDialogueStart));
            onDialogueCompleteProperty = serializedObject.FindProperty(nameof(DialogueRunner.onDialogueComplete));
            onCommandProperty = serializedObject.FindProperty(nameof(DialogueRunner.onCommand));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(yarnProjectProperty);
            EditorGUILayout.PropertyField(variableStorageProperty);

            if (variableStorageProperty.objectReferenceValue == null)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox($"An {ObjectNames.NicifyVariableName(nameof(InMemoryVariableStorage))} component will be added at run time.", MessageType.Info);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(lineProviderProperty);

            if (lineProviderProperty.objectReferenceValue == null)
            {
                var yarnProject = yarnProjectProperty.objectReferenceValue as YarnProject;
                EditorGUI.indentLevel += 1;
                if (yarnProject != null && yarnProject.localizationType == LocalizationType.Unity) {
#if USE_UNITY_LOCALIZATION
                    // If this is a project using Unity localisation, we can't add a
                    // line provider at runtime because we won't know what string
                    // table it should use. In this situation, we'll show a warning
                    // and offer a quick button they can click to add one.
                    string unityLocalizedLineProvider = ObjectNames.NicifyVariableName(nameof(UnityLocalization.UnityLocalisedLineProvider));
                    EditorGUILayout.HelpBox($"This project uses Unity Localization. You will need to add a {unityLocalizedLineProvider} for it to work. Click the button below to add one, and then set it up.", MessageType.Warning);

                    GameObject gameObject = (serializedObject.targetObject as DialogueRunner).gameObject;

                    UnityLocalization.UnityLocalisedLineProvider existingLineProvider = gameObject.GetComponent<UnityLocalization.UnityLocalisedLineProvider>();

                    if (existingLineProvider != null) {
                        if (GUILayout.Button($"Use {unityLocalizedLineProvider}")) {
                            lineProviderProperty.objectReferenceValue = existingLineProvider;
                        }
                    } else {
                        if (GUILayout.Button($"Add {unityLocalizedLineProvider}")) {
                            var lineProvider = gameObject.AddComponent<UnityLocalization.UnityLocalisedLineProvider>();

                            lineProviderProperty.objectReferenceValue = lineProvider;
                        }
                    }
                    
                    #else
                    EditorGUILayout.HelpBox($"This project uses Unity Localization, but Unity Localization is not installed. Please install it, or change this Yarn Project to use Yarn Spinner's internal localisation system.", MessageType.Error);
                    #endif

                } else {
                    // Otherwise, we'll assume they're using the built-in
                    // localisation system, and we can safely create one at
                    // runtime because we know everything we need to to set that
                    // up.
                    EditorGUILayout.HelpBox($"A {ObjectNames.NicifyVariableName(nameof(TextLineProvider))} component will be added at run time.", MessageType.Info);
                }
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(dialogueViewsProperty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(startAutomaticallyProperty);

            if (startAutomaticallyProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(startNodeProperty);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.PropertyField(runSelectedOptionAsLineProperty);
            EditorGUILayout.PropertyField(verboseLoggingProperty);

            EditorGUILayout.Space();

#if UNITY_2019_1_OR_NEWER
            ShowCallbacks = EditorGUILayout.BeginFoldoutHeaderGroup(ShowCallbacks, "Events");
#endif

            if (ShowCallbacks)
            {
                EditorGUILayout.PropertyField(onNodeStartProperty);
                EditorGUILayout.PropertyField(onNodeCompleteProperty);
                EditorGUILayout.PropertyField(onDialogueStartProperty);
                EditorGUILayout.PropertyField(onDialogueCompleteProperty);
                EditorGUILayout.PropertyField(onCommandProperty);
            }

#if UNITY_2019_1_OR_NEWER
            EditorGUILayout.EndFoldoutHeaderGroup();
#endif

            serializedObject.ApplyModifiedProperties();

        }
    }
}
