#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;

    public class SimpleCharacterAnimation : MonoBehaviour
    {
        [SerializeField] private Animator? animator;
        [SerializeField] private SimpleCharacterMovement? movement;
        [SerializeField] private string speedParameter = "Speed";

        [SerializeField] Yarn.Unity.SerializableDictionary<string, string> facialExpressions = new();
        [SerializeField] string defaultFace = "";
        [SerializeField] string facialExpressionsLayer = "Face";
        private int facialExpressionsLayerID = 0;

        public void OnValidate()
        {
            animator = GetComponentInChildren<Animator>();
            movement = GetComponentInChildren<SimpleCharacterMovement>();
        }

        protected void Awake()
        {
            if (animator != null)
            {
                facialExpressionsLayerID = animator.GetLayerIndex(facialExpressionsLayer);
            }
        }

        [YarnCommand("face")]
        public void SetFacialExpression(string name, float crossfadeTime = 0f)
        {
            if (animator == null)
            {
                Debug.LogWarning($"{name} has no {nameof(Animator)}");
                return;
            }

            if (!facialExpressions.TryGetValue(name, out var stateName))
            {
                Debug.LogWarning($"{name} is not a valid facial expression (expected {string.Join(", ", facialExpressions.Keys)})");
                return;
            }

            if (crossfadeTime <= 0)
            {
                animator.Play(stateName, facialExpressionsLayerID);
            }
            else
            {
                animator.CrossFadeInFixedTime(stateName, crossfadeTime, facialExpressionsLayerID);
            }
        }

        public void Update()
        {
            if (animator == null || movement == null || string.IsNullOrEmpty(speedParameter))
            {
                return;
            }

            animator.SetFloat(speedParameter, movement.CurrentSpeedFactor);
        }
    }
}