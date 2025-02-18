/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class ChatterGroupManager : MonoBehaviour
    {
        [SerializeField] Transform? player;

        [SerializeField] Vector2 delayBetweenChatters = new Vector2(0, 1);

        [SerializeField] DialogueRunner? primaryDialogueRunner;

        private List<ChatterGroup> chatterGroups = new();

        public void Awake()
        {
            chatterGroups.AddRange(GetComponentsInChildren<ChatterGroup>());
        }

        public void Start()
        {

            foreach (var chatterGroup in chatterGroups)
            {
                StartChatterAsync(chatterGroup).Forget();
            }
        }

        public void OnEnable()
        {
            if (primaryDialogueRunner != null)
            {
                primaryDialogueRunner.onDialogueStart?.AddListener(InterruptAllChatter);
            }
        }

        public void OnDisable()
        {
            if (primaryDialogueRunner != null)
            {
                primaryDialogueRunner.onDialogueStart?.RemoveListener(InterruptAllChatter);
            }
        }

        private async YarnTask StartChatterAsync(ChatterGroup chatterGroup)
        {
            var destroyedToken = this.destroyCancellationToken;

            try
            {
                while (destroyedToken.IsCancellationRequested == false)
                {
                    if (chatterGroup.startImmediatelyOnEnter)
                    {
                        await YarnTask.Yield();
                    }
                    else
                    {
                        // Wait a random amount of time before starting chatter.
                        var delay = UnityEngine.Random.Range(delayBetweenChatters.x, delayBetweenChatters.y);
                        await YarnTask.Delay(TimeSpan.FromSeconds(delay), destroyedToken);
                    }

                    if (primaryDialogueRunner != null && primaryDialogueRunner.IsDialogueRunning && chatterGroup.interruptedByPrimaryConversation)
                    {
                        // Primary dialogue is running, and this group should
                        // not run during a primary conversation - don't start a
                        // background conversation.
                        continue;
                    }

                    if (chatterGroup.IsRunning)
                    {
                        // The chatter group is already running. No need to start it.
                        continue;
                    }

                    if (player != null && !chatterGroup.IsInStartRange(player.position))
                    {
                        // Our listener is too far away from the group to be heard. Don't start.
                        continue;
                    }

                    // Start running chatter.
                    var runTask = chatterGroup.RunChatter();

                    // Wait until the dialogue is complete, or the player has
                    // left the stop radius.

                    bool hasNotifiedPlayerLeftRadius = false;

                    while (!runTask.IsCompleted())
                    {
                        if (destroyedToken.IsCancellationRequested)
                        {
                            // This object was destroyed; interrupt the chatter
                            // and exit.
                            chatterGroup.Interrupt();
                            return;
                        }

                        if (player != null && chatterGroup.IsOutsideStopRange(player.position) && hasNotifiedPlayerLeftRadius == false)
                        {
                            // Only notify a single time, in case it takes time
                            // for the chatter group to finish.
                            hasNotifiedPlayerLeftRadius = true;
                            chatterGroup.OnPlayerLeftStopRadius();
                        }
                        await YarnTask.Yield();
                    }

                    if (chatterGroup.startImmediatelyOnEnter && player != null)
                    {
                        // We're configured to start when the player enters the
                        // start range. Wait until the player is no longer in
                        // the range, so that we don't immediately run the
                        // chatter again as soon as it ends.
                        await YarnTask.WaitUntil(() => !chatterGroup.IsInStartRange(player.position));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This game object was destroyed; clean up by stopping the
                // background chatter
                if (chatterGroup.IsRunning)
                {
                    chatterGroup.Interrupt();
                }
            }
        }

        public void InterruptAllChatter()
        {
            // The player has started a conversation; stop all background chatters
            foreach (var chatterGroup in chatterGroups)
            {
                if (chatterGroup.interruptedByPrimaryConversation)
                {
                    chatterGroup.Interrupt();
                }
            }
        }
    }
}