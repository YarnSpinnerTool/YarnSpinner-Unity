#nullable enable
using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(EnsureInputModuleAvailable))]
    public class EnsureInputModuleAvailableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            const string message = "This component checks to see if a UI Input Module exists in the scene. If one isn't available, it creates an input module on this object that's compatible with your current input system.";

            EditorGUILayout.HelpBox(message, MessageType.Info);

            if (target is not EnsureInputModuleAvailable module)
            {
                return;
            }

            if (module.TryGetComponent<UnityEngine.EventSystems.BaseInputModule>(out var existingInputModule))
            {
                if ((existingInputModule.hideFlags & HideFlags.DontSaveInEditor) != 0)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.HelpBox("The " + existingInputModule.GetType().Name + " on this object is marked as temporary, and won't be saved in the scene. Click the button below if you'd like to include it in the saved scene.", MessageType.Info);
                    if (GUILayout.Button("Save in Scene"))
                    {
                        existingInputModule.hideFlags &= ~HideFlags.DontSaveInEditor;
                        UnityEditor.EditorUtility.SetDirty(existingInputModule.gameObject);
                        var scene = existingInputModule.gameObject.scene;
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    }
                }
            }
        }
    }
}
