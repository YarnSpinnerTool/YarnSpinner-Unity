using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

#if ENABLE_INPUT_SYSTEM
                // Create an input module that uses the Legacy Input Manager.
                this.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
                // Create an input module that uses the Input System.
                this.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            };
#endif
        }

        private static bool IsInputModuleInScene =>
            FindAnyObjectByType<UnityEngine.EventSystems.BaseInputModule>(FindObjectsInactive.Include) != null;
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
            }
        }
    }
#endif

}
