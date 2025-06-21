using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{

    /// <summary>
    /// Detects if no Input Module for the input system is present in the scene.
    /// If there isn't one, creates an input module that's compatible with the
    /// available input system - an InputSystemUIInputModule for the Input
    /// System, and a StandaloneInputModule for the legacy Input Manager. If a
    /// module is already present, no action is taken.
    /// </summary>
    internal sealed class EnsureInputModuleAvailable : MonoBehaviour
    {
        void OnValidate()
        {
            if (!IsInputModuleInScene)
            {
                // No input module was found in the scene. Try to add one.
                AddInputModule();
            }
        }

        private void AddInputModule()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                // Don't modify prefab assets themselves, only instances in the scene.
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (IsInputModuleInScene)
                {
                    // An input module was added between us scheduling the call
                    // and the call running. Nothing to do.
                    return;
                }
                UnityEngine.EventSystems.BaseInputModule inputModule;

#if ENABLE_INPUT_SYSTEM
                // Create an input module that uses the Legacy Input Manager.
                inputModule = this.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
                // Create an input module that uses the Input System.
                inputModule = this.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

                // Next, determine whether this change is something that should
                // be written to disk. We don't want to accidentally commit a
                // change that inserts a specific type of input module, so we'll
                // prevent any changes that happen in scenes that are located in
                // Packages from being saved. (If you DO want to commit a change
                // that adds an input module to a scene in Packages/, delete
                // this auto-added input module from the game object and add one
                // manually.)

                bool isInPackages = IsInPackages(this.gameObject);
                if (isInPackages)
                {
                    // This scene is in a package, and we should treat it as
                    // read-only. Mark this component as not saved in the
                    // editor.
                    inputModule.hideFlags |= HideFlags.DontSaveInEditor;
                }
                else
                {
                    // Otherwise, we're free to save this change. Mark that the
                    // game object has been modified.
                    UnityEditor.EditorUtility.SetDirty(this.gameObject);
                }
            };
#endif
        }

        internal static bool IsInPackages(GameObject gameObject)
        {
            var scene = GameObject.GetScene(gameObject.GetInstanceID());
            bool isInPackages = scene.path.StartsWith("Packages");
            return isInPackages;
        }

        private bool IsInputModuleInScene
        {
            get
            {
                return this.TryGetComponent<UnityEngine.EventSystems.BaseInputModule>(out _)
                    || FindAnyObjectByType<UnityEngine.EventSystems.BaseInputModule>(FindObjectsInactive.Include) != null;
            }
        }
    }

#if UNITY_EDITOR
    namespace Editor
    {
        using UnityEditor;
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
                            var scene = GameObject.GetScene(existingInputModule.gameObject.GetInstanceID());
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                        }
                    }
                }
            }
        }
    }
#endif

}
