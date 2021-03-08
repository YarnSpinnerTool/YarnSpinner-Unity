﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace Yarn.Unity {
    [CustomEditor(typeof(YarnLinesAsCanvasText))]
    public class YarnLinesAsCanvasTextEditor : Editor {
        
        private SerializedProperty stringsToViews;
        private SerializedProperty _yarnProgramProperty = default;
        private SerializedProperty _useLinesFromProperty;
        private SerializedProperty _localizationDatabaseProperty;
        private SerializedProperty _stringsToViewsProperty;
        
        private SerializedProperty _useTextMeshProProperty = default;

        Dictionary<string, Object> idsToTexts = new Dictionary<string, Object>();
        private bool _showTextUiComponents = true;
        private const string _textUiComponentsLabel = "Text UI Components";
        private string _lastLanguageId = default;
        private GUIStyle _headerStyle;

        private void OnEnable()
        {
            _headerStyle = new GUIStyle() { fontStyle = FontStyle.Bold };
            _lastLanguageId = Preferences.TextLanguage;

            _yarnProgramProperty = serializedObject.FindProperty("yarnProgram");
            _useLinesFromProperty = serializedObject.FindProperty("useLinesFromScript");
            _localizationDatabaseProperty = serializedObject.FindProperty("localizationDatabase");
            
            _stringsToViewsProperty = serializedObject.FindProperty("stringsToViews");

            _useTextMeshProProperty = serializedObject.FindProperty("_useTextMeshPro");

            UpdateTargetObjectMappingTable();

        }

        private void ClearMappingTable(YarnLinesAsCanvasText canvasText)
        {
            if (canvasText.stringsToViews.Count != 0)
            {
                // Modify the dictionary directly, to make Unity
                // realise that the property is dirty
                _stringsToViewsProperty.FindPropertyRelative("keys").ClearArray();

                // And clear the in-memory representation as well
                canvasText.stringsToViews.Clear();

                serializedObject.ApplyModifiedProperties();
            }
        }

        private void UpdateTargetObjectMappingTable()
        {

            var canvasText = serializedObject.targetObject as YarnLinesAsCanvasText;
            var useLinesFromTextAsset = _useLinesFromProperty.objectReferenceValue;

            if (!(_yarnProgramProperty.objectReferenceValue is YarnProject yarnProject))
            {
                // No program means no strings available, so clear it and
                // bail out
                ClearMappingTable(canvasText);
                return;
            }

            if (!(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(useLinesFromTextAsset)) is YarnImporter yarnImporter))
            {
                ClearMappingTable(canvasText);
                return;
            }

            var stringIDs = yarnImporter.stringIDs;

            var extraneousIDs = canvasText.stringsToViews.Keys.Except(stringIDs).ToList();
            var missingIDs = stringIDs.Except(canvasText.stringsToViews.Keys).ToList();

            foreach (var id in extraneousIDs)
            {
                canvasText.stringsToViews.Remove(id);
            }

            foreach (var id in missingIDs)
            {
                canvasText.stringsToViews.Add(id, null);
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope()) {
                EditorGUILayout.PropertyField(_yarnProgramProperty);
                EditorGUILayout.PropertyField(_useLinesFromProperty);
                EditorGUILayout.PropertyField(_localizationDatabaseProperty);

                if (change.changed) {
                    // Rebuild the string table if the yarn asset or the language preference has changed
                    UpdateTargetObjectMappingTable();
                }
            
            }
            
            EditorGUILayout.PropertyField(_useTextMeshProProperty);

            if (!(_yarnProgramProperty.objectReferenceValue is YarnProject)) {
                EditorGUILayout.HelpBox("This component needs a yarn script.", MessageType.Info);
            } else if (!(_localizationDatabaseProperty.objectReferenceValue is LocalizationDatabase localizationDatabase)) {
                EditorGUILayout.HelpBox("This component needs a localization database.", MessageType.Info);
             } else {
                _showTextUiComponents = EditorGUILayout.Foldout(_showTextUiComponents, _textUiComponentsLabel);
                if (_showTextUiComponents) {

                    var keysProperty = _stringsToViewsProperty.FindPropertyRelative("keys");
                    var valuesProperty = _stringsToViewsProperty.FindPropertyRelative("values");

                    if (keysProperty.arraySize == 0) {
                        EditorGUILayout.HelpBox("Couldn't find any text lines on the referenced Yarn asset.", MessageType.Info);
                    } else {

                        EditorGUI.indentLevel += 1;
                        
                        for (int i = 0; i < keysProperty.arraySize; i++) {

                            // Get properties for the elements in this dictionary
                            var keyProperty = keysProperty.GetArrayElementAtIndex(i);
                            var valueProperty = valuesProperty.GetArrayElementAtIndex(i);

                            // Draw the actual content of the yarn line as lable so the user knows what text 
                            // will placed on the referenced component
                            string key = keyProperty.stringValue;

                            // Retrieve the localized text
                            var localisedText = localizationDatabase.GetLocalization(Preferences.TextLanguage)?.GetLocalizedString(key) ?? $"<no localization for '{Preferences.TextLanguage}'>";


                            GUIContent label = new GUIContent() { 
                                text = $"'{localisedText}'"
                            };
                            System.Type textType;
                            if (_useTextMeshProProperty.boolValue) {
                                textType = typeof(TMPro.TextMeshProUGUI);                                
                            } else {
                                textType = typeof(UnityEngine.UI.Text);
                            }

                            // Is the current object in this value of the
                            // correct type? (e.g. if we're in TMP mode, is
                            // it a textmeshpro text object?)
                            bool objectTextTypeIsValid = valueProperty.objectReferenceValue?.GetType().IsAssignableFrom(textType) ?? false;

                            // Get a reference to it is, and get null if
                            // it's not
                            var displayedObject = objectTextTypeIsValid ? valueProperty.objectReferenceValue : null;

                            using (var change = new EditorGUI.ChangeCheckScope()) {
                                // Only modify valueProperty's
                                // objectReferenceValue if the field
                                // actually changed, because we don't want
                                // to throw away its contents if the user
                                // just clicks on 'use textmesh pro'
                                var newReference = EditorGUILayout.ObjectField(label, displayedObject, textType, true);

                                if (change.changed) {
                                    valueProperty.objectReferenceValue = newReference;        
                                }
                            }
                        }
                        
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
