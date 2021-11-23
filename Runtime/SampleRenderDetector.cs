using UnityEngine;

namespace Yarn.Unity
{
    // this component only exists to be added into the sample scenes
    // it detects if the render pipeline is different from the one the
    // samples were created with and warn you that things might look odd
    [ExecuteInEditMode]
    public class SampleRenderDetector : MonoBehaviour
    {
        void Awake()
        {
            // when using the built in render pipeline there is not graphics pipeline set
            // so this being null is the same as saying "using the built in pipeline"
            // there are some edgecases this won't detect but will work well enough
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset)
            {
                Debug.LogWarning("These samples were created using the built in render pipeline, things will not appear correctly. You should upgrade the materials to be compatible.");
            }
        }
    }
}
