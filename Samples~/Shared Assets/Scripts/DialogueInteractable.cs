#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using UnityEngine.Events;

    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected UnityEvent<bool>? onActiveChanged;
        [SerializeField] protected UnityEvent? onInteractionStarted;
        [SerializeField] protected UnityEvent? onInteractionEnded;

        private bool _isCurrent;

        public virtual bool IsCurrent
        {
            get => _isCurrent; set
            {
                _isCurrent = value;

                onActiveChanged?.Invoke(value);
            }
        }

        public abstract YarnTask Interact(GameObject interactor);

        public virtual bool InteractorShouldTurnToFaceWhenInteracted => false;
    }

    public class DialogueInteractable : Interactable
    {
        [SerializeField] DialogueReference dialogue = new();
        [SerializeField] DialogueRunner? dialogueRunner;

        [SerializeField] bool turnsToInteractor = true;

        public override bool InteractorShouldTurnToFaceWhenInteracted => turnsToInteractor;

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
        }

        public override async YarnTask Interact(GameObject interactor)
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
            if (dialogueRunner.IsDialogueRunning)
            {
                Debug.LogError($"Can't run dialogue {dialogue}: dialogue runner is already running");
                return;
            }

            onInteractionStarted?.Invoke();

            dialogueRunner.StartDialogue(dialogue);

            if (turnsToInteractor && TryGetComponent<SimpleCharacter>(out var character))
            {
                character.lookTarget = interactor.transform;
            }

            var destroyCancellation = destroyCancellationToken;

            await dialogueRunner.DialogueTask;

            if (destroyCancellation.IsCancellationRequested)
            {
                return;
            }

            if (turnsToInteractor && TryGetComponent<SimpleCharacter>(out character))
            {
                character.lookTarget = null;
            }
        }
    }
}
