using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Detects if the render pipeline is different from the one the samples
    /// were created with, and warn you that things might look odd.
    /// </summary>
    /// <remarks>
    /// This component only exists to be added into the Yarn Spinner sample
    /// scenes.
    /// </remarks>
    [ExecuteInEditMode]
    public class SampleRenderDetector : MonoBehaviour
    {
        void Awake()
        {
            // When using the built in render pipeline there is no graphics
            // pipeline set, so this being null is the same as saying "using the
            // built in pipeline". 
            //
            // There are some edge cases this won't detect, but will work well
            // enough.
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset)
            {
                Debug.LogWarning("These samples were created using the built in render pipeline, things will not appear correctly. You should upgrade the materials to be compatible.");
            }
        }
    }
}
