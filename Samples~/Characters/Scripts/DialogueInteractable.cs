#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using UnityEngine.Events;

    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected UnityEvent<bool>? onActiveChanged;

        private bool _isCurrent;

        public virtual bool IsCurrent
        {
            get => _isCurrent; set
            {
                _isCurrent = value;

                onActiveChanged?.Invoke(value);
            }
        }

        public abstract void Interact(SimpleCharacterInteraction interactor);

        public virtual bool InteractorShouldTurnToFaceWhenInteracted => false;
    }

    public class DialogueInteractable : Interactable
    {
        [SerializeField] DialogueReference dialogue = new();
        [SerializeField] DialogueRunner? dialogueRunner;

        [SerializeField] bool turnsToInteractor = true;
        [SerializeField] float turnSpeed = 300f;

        public override bool InteractorShouldTurnToFaceWhenInteracted => turnsToInteractor;

        private Quaternion originalRotation;
        private Transform? lookTarget;


        public void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                return;
            }
#endif

            if (dialogueRunner == null)
            {
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            }
            if (dialogueRunner != null && dialogueRunner.YarnProject != null && dialogue.project == null)
            {
                dialogue.project = dialogueRunner.YarnProject;
            }
        }

        public override bool IsCurrent
        {
            set
            {
                if (value == true)
                {
                    // We've been told we're active. Double check that we
                    // actually CAN be active based on the additional
                    // information we have about what would happen if we were
                    // interacted with.

                    if (dialogue == null || dialogue.IsValid == false)
                    {
                        // We have no dialogue reference, so we can't be interacted with.
                        return;
                    }

                    if (dialogueRunner == null)
                    {
                        // We have no dialogue runner, so we can't be interacted with.
                        onActiveChanged?.Invoke(false);
                        return;
                    }

                    // TODO: remove this once YS core is updated
                    if (dialogueRunner.Dialogue.ContentSaliencyStrategy == null)
                    {
                        dialogueRunner.Dialogue.ContentSaliencyStrategy = new Yarn.Saliency.FirstSaliencyStrategy();
                    }

                    var runnableContent = dialogueRunner.Dialogue.GetSaliencyOptionsForNodeGroup(dialogue);
                    var content = dialogueRunner.Dialogue.ContentSaliencyStrategy.QueryBestContent(runnableContent);

                    if (content == null)
                    {
                        // We have no content we can run. Don't show the indicator.
                        onActiveChanged?.Invoke(false);
                        return;
                    }
                }

                base.IsCurrent = value;
            }
        }

        protected void Awake()
        {
            IsCurrent = false;
            originalRotation = transform.rotation;
        }

        protected void Update()
        {
            if (turnsToInteractor == false)
            {
                return;
            }

            Quaternion desiredOrientation;
            if (lookTarget == null)
            {
                desiredOrientation = originalRotation;
            }
            else
            {
                var positionAtSameY = lookTarget.position;
                positionAtSameY.y = transform.position.y;
                var lookDirection = positionAtSameY - transform.position;
                desiredOrientation = Quaternion.LookRotation(lookDirection);
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredOrientation, this.turnSpeed * Time.deltaTime);
        }

        public override void Interact(SimpleCharacterInteraction interactor)
        {
            if (dialogue == null)
            {
                return;
            }
            if (dialogueRunner == null)
            {
                Debug.LogError($"Can't run dialogue {dialogue}: dialogue runner not set");
                return;
            }
            if (!dialogue.IsValid)
            {
                Debug.LogError($"Can't run dialogue {dialogue}: not a valid dialogue reference");
                return;
            }

            dialogueRunner.StartDialogue(dialogue);

            async YarnTask LookAtTargetUntilDialogueComplete()
            {
                if (dialogueRunner == null)
                {
                    return;
                }

                lookTarget = interactor.transform;
                await dialogueRunner.DialogueTask;
                lookTarget = null;
            }

            LookAtTargetUntilDialogueComplete().Forget();
        }
    }
}
