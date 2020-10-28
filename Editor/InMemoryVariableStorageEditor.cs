using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yarn.Unity;

[CustomEditor(typeof(InMemoryVariableStorage))]
public class InMemoryVariableStorageEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var varStorage = (InMemoryVariableStorage)target;
        
        varStorage.showDebug = EditorGUILayout.Foldout(varStorage.showDebug, "Debug Variables");

        if ( !varStorage.showDebug ) {
            return;
        }

        if ( !Application.isPlaying ) {
            EditorGUILayout.HelpBox("Not in Play Mode, so no variables to display!", MessageType.Info);
            return;
        }

        var style = EditorGUIUtility.GetBuiltinSkin( EditorSkin.Inspector ).label;
        var list = varStorage.GetDebugList();
        var height = style.CalcHeight( new GUIContent(list), EditorGUIUtility.currentViewWidth ) - 5;
        EditorGUILayout.SelectableLabel( list, GUILayout.MaxHeight(height), GUILayout.ExpandHeight(true) );
        
    }
}
