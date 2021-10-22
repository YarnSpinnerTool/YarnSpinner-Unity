using UnityEngine;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Yarn.Unity;

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(YarnImporter))]
    public class YarnImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty isSuccessfullyCompiledProperty;
        private SerializedProperty parseErrorMessagesProperty;
        
        public IEnumerable<string> DestinationProjectError => destinationYarnProjectImporter?.compileErrors ?? new List<string>();

        private YarnProject destinationYarnProject;
        private YarnProjectImporter destinationYarnProjectImporter;

        public override void OnEnable()
        {
            base.OnEnable();

            isSuccessfullyCompiledProperty = serializedObject.FindProperty(nameof(YarnImporter.isSuccessfullyParsed));
            parseErrorMessagesProperty = serializedObject.FindProperty(nameof(YarnImporter.parseErrorMessages));
            
            UpdateDestinationProject();
        }

        private void UpdateDestinationProject()
        {
            destinationYarnProject = (target as YarnImporter).DestinationProject;

            if (destinationYarnProject != null)
            {
                destinationYarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(destinationYarnProject)) as YarnProjectImporter;
            }
        }

        public override void OnInspectorGUI()
        {

            serializedObject.Update();
            EditorGUILayout.Space();

            // If there's a parse error in any of the selected objects,
            // show an error. If the selected objects have the same
            // destination program, and there's a compile error in it, show
            // that. 
            if (parseErrorMessagesProperty.arraySize > 0)
            {
                if (serializedObject.isEditingMultipleObjects)
                {
                    EditorGUILayout.HelpBox("Some of the selected scripts have errors.", MessageType.Error);
                }
                else
                {
                    foreach (SerializedProperty errorProperty in parseErrorMessagesProperty) {
                        EditorGUILayout.HelpBox(errorProperty.stringValue, MessageType.Error);
                    }
                }
            }
            else if (DestinationProjectError.Count() > 0)
            {
                var displayMessage = string.Join("\n", DestinationProjectError);
                EditorGUILayout.HelpBox(displayMessage, MessageType.Error);
            }

            if (destinationYarnProject == null)
            {
                EditorGUILayout.HelpBox("This script is not currently part of a Yarn Project, so it can't be compiled or loaded into a Dialogue Runner. Either click Create New Yarn Project, or add a Yarn project to the field below.", MessageType.Info);
                if (GUILayout.Button("Create New Yarn Project..."))
                {
                    YarnProjectUtility.CreateYarnProject(target as YarnImporter);

                    UpdateDestinationProject();

                }
            }
            
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var project = EditorGUILayout.ObjectField("Project", destinationYarnProject, typeof(YarnProject), false);

                if (change.changed) {
                    string programPath = null;
                    if (project != null) {
                        programPath = AssetDatabase.GetAssetPath(project);
                    }
                    YarnProjectUtility.AssignScriptToProject( (target as YarnImporter).assetPath, programPath);
                    
                    UpdateDestinationProject();
                }

            }

            EditorGUILayout.Space();

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
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that
            // works identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }



    }
}
