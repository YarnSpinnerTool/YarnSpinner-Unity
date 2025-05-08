/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

#nullable enable

namespace Yarn.Unity.Samples
{
    /// <summary>
    /// Detects if the render pipeline is different from the one the samples
    /// were created with, and warn you that things might look odd.
    /// </summary>
    /// <remarks>
    /// This component only exists to be added into the Yarn Spinner sample
    /// scenes.
    /// You are safe to delete this.
    /// </remarks>
    [ExecuteInEditMode]
    public sealed class SampleRenderDetector : MonoBehaviour
    {
        void Awake()
        {
            // When using the built in render pipeline there is no graphics
            // pipeline set, so this being null is the same as saying "using the
            // built in pipeline". 
            //
            // There are some edge cases this won't detect, but will work well
            // enough.
            if (Application.isEditor && UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null)
            {
                Debug.LogWarning("The samples were created using the Universal Render Pipeline, things will not appear correctly. You will need to convert the materials to be compatible.");
            }
        }
    }

#if UNITY_EDITOR
    namespace Editor
    {
        using UnityEditor;


        [CustomEditor(typeof(SampleRenderDetector))]
        public class SampleRenderDetectorEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null)
                {
                    EditorGUILayout.HelpBox("The samples were created using the Universal Render Pipeline, things will not appear correctly.\nYou are safe to delete this game object.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("This object detects if samples were created using the URP.\nYou are safe to delete this game object.", MessageType.Info);
                }
            }
        }
    }
#endif
}
