/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{

    [CustomEditor(typeof(InMemoryVariableStorage))]
    public class InMemoryVariableStorageEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var varStorage = (InMemoryVariableStorage)target;

            varStorage.showDebug = EditorGUILayout.Foldout(varStorage.showDebug, "Debug Variables");

            if (!varStorage.showDebug)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Not in Play Mode, so no variables to display!", MessageType.Info);
                return;
            }

            var style = EditorStyles.label;
            var list = varStorage.GetDebugList();
            var height = style.CalcHeight(new GUIContent(list), EditorGUIUtility.currentViewWidth);
            EditorGUILayout.SelectableLabel(list, GUILayout.MaxHeight(height), GUILayout.ExpandHeight(true));

        }
    }

}
