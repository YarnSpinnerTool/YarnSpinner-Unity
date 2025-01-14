#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using UnityEngine.Events;

    public class AnimationVariable : MonoBehaviour
    {
        [SerializeField] DialogueRunner? dialogueRunner;

        [SerializeField] Animator? animator;

        public enum VariableType
        {
            Float, Int, Bool, Trigger
        }

        [SerializeField] VariableType Type = VariableType.Bool;

        [SerializeField] string variable = string.Empty;
        [SerializeField] string parameter = string.Empty;

        System.IDisposable? listener = null;

        public void OnValidate()
        {
            if (this.animator == null)
            {
                this.animator = GetComponentInChildren<Animator>();
            }
        }

        public void OnEnable()
        {
            if (dialogueRunner == null || animator == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(variable) || string.IsNullOrEmpty(parameter))
            {
                return;
            }

#if DEBUG
            var foundParameter = false;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == this.parameter)
                {
                    foundParameter = true;
                    break;
                }
            }

            if (!foundParameter)
            {
                Debug.LogWarning($"Failed to find parameter {parameter}", this);
            }
#endif

            listener?.Dispose();

            var parameterID = Animator.StringToHash(parameter);

            switch (Type)
            {
                case VariableType.Float:
                    listener = dialogueRunner.VariableStorage.AddChangeListener<float>(variable, newVal => animator.SetFloat(parameterID, newVal));
                    break;
                case VariableType.Int:
                    listener = dialogueRunner.VariableStorage.AddChangeListener<float>(variable, newVal =>
                    {
                        animator.SetInteger(parameterID, (int)newVal);
                    });
                    break;
                case VariableType.Bool:
                    listener = dialogueRunner.VariableStorage.AddChangeListener<bool>(variable, newVal => animator.SetBool(parameterID, newVal));
                    break;
                case VariableType.Trigger:
                    listener = dialogueRunner.VariableStorage.AddChangeListener<bool>(variable, newVal =>
                    {
                        if (newVal)
                        {
                            animator.SetTrigger(parameterID);
                        }
                    });
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(Type));
            }
        }

        public void OnDisable()
        {
            listener?.Dispose();
            listener = null;
        }

    }
}
