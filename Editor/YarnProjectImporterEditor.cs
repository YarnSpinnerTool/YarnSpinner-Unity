using System.Collections.Generic;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using System.Linq;
using Yarn.Compiler;
using System.IO;
using UnityEditorInternal;
using System.Collections;

namespace Yarn.Unity
{
    [CustomEditor(typeof(YarnProjectImporter))]
    public class YarnProjectImporterEditor : ScriptedImporterEditor
    {

        private SerializedProperty compileErrorProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty sourceScriptsProperty;
        private SerializedProperty languagesToSourceAssetsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;

        public override void OnEnable()
        {
            base.OnEnable();
            sourceScriptsProperty = serializedObject.FindProperty("sourceScripts");
            compileErrorProperty = serializedObject.FindProperty("compileError");
            serializedDeclarationsProperty = serializedObject.FindProperty("serializedDeclarations");

            languagesToSourceAssetsProperty = serializedObject.FindProperty("languagesToSourceAssets");

            serializedDeclarationsList = new ReorderableDeclarationsList(serializedObject, serializedDeclarationsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This Yarn Project has no content. Add Yarn Scripts to it.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty, true);

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(compileErrorProperty.stringValue) == false)
            {
                EditorGUILayout.HelpBox(compileErrorProperty.stringValue, MessageType.Error);
            }

            serializedDeclarationsList.DrawLayout();

            EditorGUILayout.PropertyField(languagesToSourceAssetsProperty);

            YarnProjectImporter yarnProjectImporter = serializedObject.targetObject as YarnProjectImporter;

            // Ask the project importer if it can generate a strings table.
            // This involves querying several assets, which means various
            // exceptions might get thrown, which we'll catch and log (if
            // we're in debug mode).
            bool canGenerateStringsTable;
            try
            {
                canGenerateStringsTable = yarnProjectImporter.CanGenerateStringsTable;
            }
            catch (System.Exception e)
            {
#if YARNSPINNER_DEBUG
                Debug.LogWarning($"Encountered in error when checking to see if Yarn Project Importer could generate a strings table: {e}", this);
#endif
                canGenerateStringsTable = false;
            }

            using (new EditorGUI.DisabledScope(canGenerateStringsTable == false))
            {
                if (GUILayout.Button("Export Strings as CSV"))
                {
                    var currentPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                    var currentFileName = Path.GetFileNameWithoutExtension(currentPath);
                    var currentDirectory = Path.GetDirectoryName(currentPath);

                    var destinationPath = EditorUtility.SaveFilePanel("Export Strings CSV", currentDirectory, $"{currentFileName}.csv", "csv");

                    if (string.IsNullOrEmpty(destinationPath) == false)
                    {
                        // Generate the file on disk
                        YarnProjectUtility.WriteStringsFile(destinationPath, yarnProjectImporter);

                        // destinationPath may have been inside our Assets
                        // directory, so refresh the asset database
                        AssetDatabase.Refresh();
                    }
                }
                if (yarnProjectImporter.languagesToSourceAssets.Count > 0)
                {
                    if (GUILayout.Button("Update Existing Strings Files"))
                    {
                        YarnProjectUtility.UpdateLocalizationCSVs(yarnProjectImporter);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(canGenerateStringsTable == true))
            {
                if (GUILayout.Button("Add Line Tags to Scripts"))
                {
                    YarnProjectUtility.AddLineTagsToFilesInYarnProject(yarnProjectImporter);
                }
            }

            var hadChanges = serializedObject.ApplyModifiedProperties();

            if (hadChanges)
            {
                Debug.Log($"{nameof(YarnProjectImporterEditor)} had changes");
            }

#if UNITY_2018
            // Unity 2018's ApplyRevertGUI is buggy, and doesn't
            // automatically detect changes to the importer's
            // serializedObject. This means that we'd need to track the
            // state of the importer, and don't have a way to present a
            // Revert button. 
            //
            // Rather than offer a broken experience, on Unity 2018 we
            // immediately reimport the changes. This is slow (we're
            // serializing and writing the asset to disk on every property
            // change!) but ensures that the writes are done.
            if (hadChanges)
            {
                // Manually perform the same tasks as the 'Apply' button
                // would
                ApplyAndImport();
            }
#endif

#if UNITY_2019_1_OR_NEWER
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that
            // works identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }



        protected override void Apply()
        {
            base.Apply();

            // Get all declarations that came from this program
            var thisProgramDeclarations = new List<Yarn.Compiler.Declaration>();

            for (int i = 0; i < serializedDeclarationsProperty.arraySize; i++)
            {
                var decl = serializedDeclarationsProperty.GetArrayElementAtIndex(i);
                if (decl.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null)
                {
                    continue;
                }

                var name = decl.FindPropertyRelative("name").stringValue;

                SerializedProperty typeProperty = decl.FindPropertyRelative("type");

                Type type = (Yarn.Type)typeProperty.enumValueIndex;

                var description = decl.FindPropertyRelative("description").stringValue;

                object defaultValue;
                switch (type)
                {
                    case Yarn.Type.Number:
                        defaultValue = decl.FindPropertyRelative("defaultValueNumber").floatValue;
                        break;
                    case Yarn.Type.String:
                        defaultValue = decl.FindPropertyRelative("defaultValueString").stringValue;
                        break;
                    case Yarn.Type.Bool:
                        defaultValue = decl.FindPropertyRelative("defaultValueBool").boolValue;
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException($"Invalid declaration type {type}");
                }

                var declaration = Declaration.CreateVariable(name, defaultValue, description);

                thisProgramDeclarations.Add(declaration);
            }

            var output = Yarn.Compiler.Utility.GenerateYarnFileWithDeclarations(thisProgramDeclarations, "Program");

            var importer = target as YarnProjectImporter;
            File.WriteAllText(importer.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(importer.assetPath);
        }
    }

}
