/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#nullable enable

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(YarnProject))]
    public class YarnProjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var importer = AssetImporter.GetAtPath(assetPath) as YarnProjectImporter;

            ProjectImportData? importData = null;
            if (importer == null)
            {
                // No importer found for this asset. Possibly it's not an asset on disk?
                return new Label("Yarn Project has no importer");
            }

            var ui = new VisualElement();

            importData = importer.ImportData;

            if (importData == null)
            {
                return new Label("Project failed to import, or needs upgrading.");
            }

            Foldout foldout;

            var importDataSO = new SerializedObject(importData);

            var yarnScriptsProperty = importDataSO.FindProperty(nameof(ProjectImportData.yarnFiles));
            var yarnScriptsField = new PropertyField(yarnScriptsProperty, "Yarn Scripts");
            yarnScriptsField.Bind(importDataSO);
            ui.Add(yarnScriptsField);
            foldout = yarnScriptsField.Q<Foldout>();
            if (foldout != null)
            {
                foldout.value = true;
            }

            var variablesProperty = importDataSO.FindProperty(nameof(ProjectImportData.serializedDeclarations));
            var variablesField = new PropertyField(variablesProperty, "Variables");
            variablesField.Bind(importDataSO);
            foldout = variablesField.Q<Foldout>();
            if (foldout != null)
            {
                foldout.value = true;
            }

            ui.Add(variablesField);

            return ui;
        }
    }

}
