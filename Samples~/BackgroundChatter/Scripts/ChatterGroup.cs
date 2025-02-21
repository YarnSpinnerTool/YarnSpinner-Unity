/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Yarn.Saliency;

    // An example of a custom saliency strategy that chooses a random piece of
    // content, regardless of its complexity
    class RandomSaliencyStrategy : IContentSaliencyStrategy
    {
        // This strategy doesn't need to take any action when content is selected
        public void ContentWasSelected(ContentSaliencyOption content) { }

        // Selects a random valid piece of content
        public ContentSaliencyOption? QueryBestContent(IEnumerable<ContentSaliencyOption> content)
        {
            // Keep only content that hasn't failed any conditions
            content = content.Where(c => c.FailingConditionValueCount == 0);

            if (!content.Any())
            {
                // Return null if no content passes this test
                return null;
            }

            // Select a random element from the remainder
            return content.RandomElement();
        }
    }

    public class ChatterGroup : MonoBehaviour
    {
        public enum OutOfRangeBehaviour
        {
            DoNothing,
            Stop,
            StopAndRunNode,
        }

        public enum SaliencyType
        {
            Random, First, Best, RandomBestLeastRecentlyViewed, BestLeastRecentlyViewed
        }

        public bool startImmediatelyOnEnter;

        [SerializeField] SaliencyType saliencyType = SaliencyType.RandomBestLeastRecentlyViewed;

        [SerializeField] OutOfRangeBehaviour outOfRangeBehaviour = OutOfRangeBehaviour.Stop;

        [UnityEngine.Serialization.FormerlySerializedAs("radius")]
        [SerializeField] float startRadius;
        [SerializeField] float stopRadius;

        [SerializeField] DialogueRunner? dialogueRunner;

        [SerializeField] DialogueReference? dialogue;

        [Tooltip("The node to run when dialogue is running and the user walks away. Only used when " + nameof(outOfRangeBehaviour) + " is " + nameof(OutOfRangeBehaviour.StopAndRunNode) + ".")]
        [SerializeField] DialogueReference? outOfRangeDialogue;

        public bool interruptedByPrimaryConversation = true;

        public void Awake()
        {
            if (dialogueRunner == null)
            {
                return;
            }

            IContentSaliencyStrategy strategy;

            switch (this.saliencyType)
            {
                case SaliencyType.Random:
                    strategy = new RandomSaliencyStrategy();
                    break;
                case SaliencyType.RandomBestLeastRecentlyViewed:
                    strategy = new RandomBestLeastRecentlyViewedSaliencyStrategy(dialogueRunner.VariableStorage);
                    break;
                case SaliencyType.BestLeastRecentlyViewed:
                    strategy = new BestLeastRecentlyViewedSaliencyStrategy(dialogueRunner.VariableStorage);
                    break;
                case SaliencyType.Best:
                    strategy = new BestSaliencyStrategy();
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unhandled strategy {this.saliencyType}");
            }

            dialogueRunner.Dialogue.ContentSaliencyStrategy = strategy;
        }


        public void OnDrawGizmosSelected()
        {
            var color = Color.yellow;
            color.a = 0.5f;
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, startRadius);

            if (stopRadius > startRadius)
            {
                color = Color.blue;
                color.a = 0.25f;
                Gizmos.color = color;
                Gizmos.DrawSphere(transform.position, stopRadius);
            }
        }

        internal YarnTask RunChatter()
        {
            if (dialogueRunner == null || dialogue == null || dialogue.IsValid == false)
            {
                Debug.LogWarning($"Chatter group can't start dialogue: dialogue runner not set, or dialogue reference not valid", this);
                return YarnTask.CompletedTask;
            }

            if (dialogueRunner.IsDialogueRunning)
            {
                Debug.LogWarning($"Chatter group can't start dialogue: dialogue runner is already running", this);
                return YarnTask.CompletedTask;
            }

            dialogueRunner.SetProject(dialogue.project);
            dialogueRunner.StartDialogue(dialogue.nodeName);

            return dialogueRunner.DialogueTask;
        }

        public void Interrupt()
        {
            if (dialogueRunner != null)
            {
                // Stop the dialogue immediately.
                dialogueRunner.Stop();
            }
        }

        internal bool IsInStartRange(Vector3 position)
        {
            return Vector3.Distance(this.transform.position, position) <= startRadius;
        }

        internal bool IsOutsideStopRange(Vector3 position)
        {
            return Vector3.Distance(this.transform.position, position) >= stopRadius;
        }

        internal void OnPlayerEnteredStartRadius()
        {
            if (startImmediatelyOnEnter)
            {

            }
        }

        internal void OnPlayerLeftStopRadius()
        {
            switch (this.outOfRangeBehaviour)
            {
                case OutOfRangeBehaviour.DoNothing:
                    // No-op.
                    break;
                case OutOfRangeBehaviour.Stop:
                    // Stop the dialogue runner if it's running.
                    if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
                    {
                        dialogueRunner.Stop();
                    }
                    break;
                case OutOfRangeBehaviour.StopAndRunNode:
                    // Stop the dialogue runner if it's running, and run a new
                    // node if we have one.
                    if (dialogueRunner != null)
                    {
                        if (dialogueRunner.IsDialogueRunning)
                        {
                            dialogueRunner.Stop();
                        }

                        if (outOfRangeDialogue != null && outOfRangeDialogue.IsValid)
                        {
                            dialogueRunner.SetProject(outOfRangeDialogue.project);
                            dialogueRunner.StartDialogue(outOfRangeDialogue.nodeName);
                        }
                    }
                    break;
            }
        }

        public bool IsRunning => dialogueRunner != null && dialogueRunner.IsDialogueRunning;
    }

}