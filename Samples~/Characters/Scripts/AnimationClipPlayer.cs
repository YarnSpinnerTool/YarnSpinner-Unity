using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Yarn.Unity.Samples
{

    [RequireComponent(typeof(Animator))]
    public class AnimationClipPlayer : MonoBehaviour
    {

        [SerializeField] AnimationClip clip;
        PlayableGraph playableGraph;

        protected void OnEnable()
        {
            AnimationPlayableUtilities.PlayClip(GetComponent<Animator>(), clip, out playableGraph);
        }

        protected void OnDisable()
        {
            // Destroys all Playables and Outputs created by the graph.
            playableGraph.Destroy();
        }
    }
}