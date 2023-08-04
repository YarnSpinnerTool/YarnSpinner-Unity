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
        public IEnumerable<string> DestinationProjectErrors => destinationYarnProjectImporters.SelectMany(i => i.GetErrorsForScript(assetTarget as TextAsset)) ?? new List<string>();

        private IEnumerable<YarnProject> destinationYarnProjects;
        private IEnumerable<YarnProjectImporter> destinationYarnProjectImporters;

        public bool HasErrors => DestinationProjectErrors.Any();

        public override void OnEnable()
        {
            base.OnEnable();

            UpdateDestinationProjects();
        }

        private void UpdateDestinationProjects()
        {
            destinationYarnProjects = (target as YarnImporter).DestinationProjects;

            if (destinationYarnProjects != null)
            {
                destinationYarnProjectImporters = destinationYarnProjects.Select(project => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter);
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
            if (HasErrors)
            {
                if (serializedObject.isEditingMultipleObjects)
                {
                    EditorGUILayout.HelpBox("Some of the selected scripts have errors.", MessageType.Error);
                }
                else
                {
                    foreach (string error in DestinationProjectErrors) {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                }
            }

            if (destinationYarnProjects.Any() != false)
            {
                if (destinationYarnProjects.Count() == 1) {
                    EditorGUILayout.ObjectField("Project", destinationYarnProjects.First(), typeof(YarnProject), false);
                } else {
                    EditorGUILayout.LabelField("Projects", EditorStyles.boldLabel);
                    EditorGUI.indentLevel += 1;
                    foreach (var project in destinationYarnProjects) {
                        EditorGUILayout.ObjectField(project, typeof(YarnProject), false);
                    }
                    EditorGUI.indentLevel -= 1;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This script is not currently part of a Yarn Project, so it can't be compiled or loaded into a Dialogue Runner. Either click Create New Yarn Project, or add this folder to an existing Yarn Project's sources list.", MessageType.Info);
                if (GUILayout.Button("Create New Yarn Project..."))
                {
                    YarnProjectUtility.CreateYarnProject(target as YarnImporter);

                    UpdateDestinationProjects();

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
