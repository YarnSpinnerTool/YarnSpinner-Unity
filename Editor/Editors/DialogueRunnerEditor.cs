using UnityEngine;
using UnityEditor;
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
        private SerializedProperty automaticallyContinueLinesProperty;
        private SerializedProperty runSelectedOptionAsLineProperty;
        private SerializedProperty lineProviderProperty;
        private SerializedProperty verboseLoggingProperty;
        private SerializedProperty onNodeStartProperty;
        private SerializedProperty onNodeCompleteProperty;
        private SerializedProperty onDialogueCompleteProperty;
        private SerializedProperty onCommandProperty;

        private void OnEnable()
        {
            yarnProjectProperty = serializedObject.FindProperty(nameof(DialogueRunner.yarnProject));
            variableStorageProperty = serializedObject.FindProperty(nameof(DialogueRunner._variableStorage));
            dialogueViewsProperty = serializedObject.FindProperty(nameof(DialogueRunner.dialogueViews));
            startNodeProperty = serializedObject.FindProperty(nameof(DialogueRunner.startNode));
            startAutomaticallyProperty = serializedObject.FindProperty(nameof(DialogueRunner.startAutomatically));
            automaticallyContinueLinesProperty = serializedObject.FindProperty(nameof(DialogueRunner.automaticallyContinueLines));
            runSelectedOptionAsLineProperty = serializedObject.FindProperty(nameof(DialogueRunner.runSelectedOptionAsLine));
            lineProviderProperty = serializedObject.FindProperty(nameof(DialogueRunner.lineProvider));
            verboseLoggingProperty = serializedObject.FindProperty(nameof(DialogueRunner.verboseLogging));
            onNodeStartProperty = serializedObject.FindProperty(nameof(DialogueRunner.onNodeStart));
            onNodeCompleteProperty = serializedObject.FindProperty(nameof(DialogueRunner.onNodeComplete));
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
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox($"A {ObjectNames.NicifyVariableName(nameof(TextLineProvider))} component will be added at run time.", MessageType.Info);
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
            EditorGUILayout.PropertyField(automaticallyContinueLinesProperty);
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
