/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Yarn.Unity;

#nullable enable

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(YarnImporter))]
    public class YarnImporterEditor : ScriptedImporterEditor
    {
        public IEnumerable<string> DestinationProjectErrors
        {
            get
            {
                if (assetTarget is not TextAsset textAsset)
                {
                    throw new System.InvalidOperationException($"Internal error: {nameof(assetTarget)} is not a {nameof(TextAsset)}");
                }

                return destinationYarnProjectImporters.SelectMany(i => i.GetErrorsForScript(textAsset)) ?? new List<string>();
            }
        }

        private IEnumerable<YarnProject>? destinationYarnProjects;
        private IEnumerable<YarnProjectImporter>? destinationYarnProjectImporters;

        public bool HasErrors => DestinationProjectErrors.Any();

        public override void OnEnable()
        {
            base.OnEnable();

            UpdateDestinationProjects();
        }

        private void UpdateDestinationProjects()
        {
            if (target is not YarnImporter yarnImporter)
            {
                throw new System.InvalidOperationException($"Internal error: target is not {nameof(YarnImporter)}");
            }
            destinationYarnProjects = yarnImporter.DestinationProjects;

            if (destinationYarnProjects != null)
            {
                destinationYarnProjectImporters = destinationYarnProjects
                    .Select(project => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)))
                    .Where(importer => importer != null)
                    .OfType<YarnProjectImporter>();


            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // nice header bit with logo and links
            DialogueRunnerEditor.DrawYarnSpinnerHeader();

            if (target is not YarnImporter yarnImporter)
            {
                EditorGUILayout.HelpBox($"Internal error: target is not a {nameof(YarnImporter)}", MessageType.Error);
                return;
            }

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
                    foreach (string error in DestinationProjectErrors)
                    {
                        EditorGUILayout.HelpBox(error, MessageType.Error);
                    }
                }
            }

            if (destinationYarnProjects != null && destinationYarnProjects.Any() != false)
            {
                if (destinationYarnProjects.Count() == 1)
                {
                    EditorGUILayout.ObjectField("Project", destinationYarnProjects.First(), typeof(YarnProject), false);
                }
                else
                {
                    EditorGUILayout.LabelField("Projects", EditorStyles.boldLabel);
                    EditorGUI.indentLevel += 1;
                    foreach (var project in destinationYarnProjects)
                    {
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
                    YarnProjectUtility.CreateYarnProject(yarnImporter);
                    UpdateDestinationProjects();
                }
            }

            var settings = YarnSpinnerProjectSettings.GetOrCreateSettings();
            if (settings.enableDirectLinkToVSCode)
            {
                if (GUILayout.Button("Open in VS Code"))
                {
                    // https://code.visualstudio.com/docs/configure/command-line#_opening-vs-code-with-urls
                    // this implies we should be able to open directly to the line and column
                    // which would be great for errors
                    // or if we show what nodes are inside this file we could jump to them directly
                    // something to explore later

                    // as both the dataPath and assetPath include the Assets folder we need to strip that off before we combine these
                    var absolutePathToYarnFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), yarnImporter.assetPath);

                    // This approach is bit weird to look at but it gets around a difference of what a URL is between VSCode and C#
                    // The initial thought is "why not use System.Uri.EscapeDataString?"
                    // it encodes "/" as "%2F" and "\" as "%5C" which is technically correct
                    // but vscode doesn't want that it just needs the spaces replaced
                    // so instead we just manually replace all the spaces with "%20"
                    // not the best but it works... for now...
                    absolutePathToYarnFile = absolutePathToYarnFile.Replace(" ", $"%20");

                    var vscodeURL = $"vscode://file{absolutePathToYarnFile}";
                    Application.OpenURL(vscodeURL);
                }
            }

            EditorGUILayout.Space();

            _ = serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
