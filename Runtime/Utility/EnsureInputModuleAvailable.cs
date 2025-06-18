using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{

    internal sealed class EnsureInputModuleAvailable : MonoBehaviour
    {

        void Awake()
        {
            if (IsInputModuleInScene)
            {
                // No input module was found in the scene. Create one that uses the Legacy Input Manager.
                this.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static bool IsInputModuleInScene =>
            FindAnyObjectByType<UnityEngine.EventSystems.BaseInputModule>(FindObjectsInactive.Include) == null;
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
                const string message = "This component checks to see if a UI Input Module exists in the scene. If one isn't available, it creates a " + nameof(UnityEngine.EventSystems.StandaloneInputModule) + ".";

                // const 
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }
    }
#endif

}
