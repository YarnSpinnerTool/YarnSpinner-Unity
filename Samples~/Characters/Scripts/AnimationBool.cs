#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using UnityEngine.Events;

    public class AnimationBool : MonoBehaviour
    {
        [SerializeField] private Animator? animator;
        [SerializeField] string parameter = "Visible";

        public void OnValidate()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public void SetValue(bool value)
        {
            if (animator != null)
            {
                animator.SetBool(parameter, value);
            }
        }

        [YarnCommand("turn_on")]
        public void TurnOn()
        {
            SetValue(true);
        }

        [YarnCommand("turn_off")]
        public void TurnOff()
        {
            SetValue(false);
        }
    }
}
