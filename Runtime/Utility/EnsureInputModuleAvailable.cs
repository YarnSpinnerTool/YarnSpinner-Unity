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
            AddInputModule();
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
                UnityEngine.EventSystems.BaseInputModule? inputModule = ActiveInputSystem;
                if (inputModule != null)
                {
                    // there is an input system in the scene now, but it might not be the right one
                    if (IsUsingCorrectInputSystem(inputModule))
                    {
                        // it is the right one, so we can just jump over this
                        // either there was always one in the scene or one was added in between this call being scheduled and run
                        return;
                    }

                    // the input system we have is the wrong type
                    // so we will need to destroy it and then make a new one
                    DestroyImmediate(inputModule);
                }

                // because we are doing this as a delayed action it's possible it will run after entering/exiting playback
                // in which case the temporary version that unity makes for in-editor playing will be destroyed
                // and we don't want that
                if (this == null)
                {
                    return;
                }
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
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
            var scene = gameObject.scene;
            bool isInPackages = scene.path.StartsWith("Packages");
            return isInPackages;
        }

        // if we have an input system this will return it
        private UnityEngine.EventSystems.BaseInputModule? ActiveInputSystem
        {
            get
            {
                // because we are doing this called as part of a delayed action it's possible it will run after entering/exiting playback
                // in which case the temporary version that unity makes for in-editor playing will be destroyed
                // and we don't want that
                if (this == null)
                {
                    return null;
                }
                if (this.TryGetComponent<UnityEngine.EventSystems.BaseInputModule>(out var inputSystem))
                {
                    return inputSystem;
                }
                return FindAnyObjectByType<UnityEngine.EventSystems.BaseInputModule>(FindObjectsInactive.Include);
            }
        }

        private bool IsUsingCorrectInputSystem(UnityEngine.EventSystems.BaseInputModule system)
        {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            return system is UnityEngine.InputSystem.UI.InputSystemUIInputModule;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return system is UnityEngine.EventSystems.StandaloneInputModule;
#else
            return false;
#endif
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
                            var scene = existingInputModule.gameObject.scene;
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                        }
                    }
                }
            }
        }
    }
#endif

}
