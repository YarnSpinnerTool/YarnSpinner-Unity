using System.Collections.Generic;
using UnityEditor;
#if UNITY_2020_1_OR_NEWER
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
    [CustomEditor(typeof(YarnProgramImporter))]
    public class YarnProgramImporterEditor : ScriptedImporterEditor
    {

        private SerializedProperty compileErrorProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty sourceScriptsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;

        public override void OnEnable()
        {
            base.OnEnable();
            sourceScriptsProperty = serializedObject.FindProperty("sourceScripts");
            compileErrorProperty = serializedObject.FindProperty("compileError");
            serializedDeclarationsProperty = serializedObject.FindProperty("serializedDeclarations");

            serializedDeclarationsList = new ReorderableDeclarationsList(serializedObject, serializedDeclarationsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(compileErrorProperty.stringValue) == false)
            {
                EditorGUILayout.HelpBox(compileErrorProperty.stringValue, MessageType.Error);
            }

            serializedDeclarationsList.DrawLayout();

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This Yarn Program has no content. Add Yarn Scripts to it.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("This Yarn Program is currently using the following scripts. It will automatically refresh when they change. If you've made a change elsewhere and need to update this Yarn Program, click Update.", MessageType.Info);

                if (GUILayout.Button("Update"))
                {
                    (serializedObject.targetObject as YarnProgramImporter).SaveAndReimport();
                }
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty, true);



            var hadChanges = serializedObject.ApplyModifiedProperties();

#if UNITY_2018
            // Unity 2018's ApplyRevertGUI is buggy, and doesn't automatically
            // detect changes to the importer's serializedObject. This means
            // that we'd need to track the state of the importer, and don't
            // have a way to present a Revert button. 
            //
            // Rather than offer a broken experience, on Unity 2018 we
            // immediately reimport the changes. This is slow (we're
            // serializing and writing the asset to disk on every property
            // change!) but ensures that the writes are done.
            if (hadChanges)
            {
                // Manually perform the same tasks as the 'Apply' button would
                ApplyAndImport();
            }
#endif

#if UNITY_2019_1_OR_NEWER
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that works
            // identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }

        protected override void Apply()
        {
            base.Apply();

            // Get all declarations that came from this program
            var thisProgramDeclarations = new List<Yarn.Compiler.Declaration>();

            for (int i = 0; i < serializedDeclarationsProperty.arraySize; i++) {
                var decl = serializedDeclarationsProperty.GetArrayElementAtIndex(i);
                if (decl.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null) {
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

            var importer = target as YarnProgramImporter;
            File.WriteAllText(importer.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(importer.assetPath);            
        }
    }

}
