/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yarn.Unity.Example
{
    public class PlayerCharacter : MonoBehaviour
    {
        public float minPosition = -5.3f;
        public float maxPosition = 5.3f;

        public float moveSpeed = 1.0f;

        public float interactionRadius = 2.0f;

        public float movementFromButtons { get; set; }

        // because we are using the same button press for both starting and skipping dialogue they collide
        // so we are going to make it so that the input gets turned off
        private DialogueAdvanceInput dialogueInput;

        void Start()
        {
            dialogueInput = FindObjectOfType<DialogueAdvanceInput>();
            dialogueInput.enabled = false;
        }

        /// <summary>
        /// Draw the range at which we'll start talking to people.
        /// </summary>
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            // Flatten the sphere into a disk, which looks nicer in 2D
            // games
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1, 1, 0));

            // Need to draw at position zero because we set position in the
            // line above
            Gizmos.DrawWireSphere(Vector3.zero, interactionRadius);
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        void Update()
        {
            // Remove all player control when we're in dialogue
            if (FindObjectOfType<DialogueRunner>().IsDialogueRunning == true)
            {
                return;
            }

            // every time we LEAVE dialogue we have to make sure we disable the input again
            if (dialogueInput.enabled)
            {
                dialogueInput.enabled = false;
            }

            // Move the player, clamping them to within the boundaries of
            // the level.
            var movement = 0f;
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            movement += Keyboard.current.rightArrowKey.isPressed ? 1f : 0f;
            movement += Keyboard.current.leftArrowKey.isPressed ? -1f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER || UNITY_2018
            movement += Input.GetAxis("Horizontal");
#endif

            movement += movementFromButtons;
            movement *= (moveSpeed * Time.deltaTime);

            var newPosition = transform.position;
            newPosition.x += movement;
            newPosition.x = Mathf.Clamp(newPosition.x, minPosition, maxPosition);

            transform.position = newPosition;

            // Detect if we want to start a conversation
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            if (Keyboard.current.spaceKey.wasPressedThisFrame) {
                CheckForNearbyNPC ();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyUp(KeyCode.Space))
            {
                CheckForNearbyNPC();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene("MainMenu");
            }
#endif
        }

        /// <summary>
        /// Find all DialogueParticipants
        /// </summary>
        /// <remarks>
        /// Filter them to those that have a Yarn start node and are in
        /// range; then start a conversation with the first one
        /// </remarks>
        public void CheckForNearbyNPC()
        {
            var allParticipants = new List<NPC>(FindObjectsOfType<NPC>());
            var target = allParticipants.Find(delegate (NPC p)
            {
                return string.IsNullOrEmpty(p.talkToNode) == false && // has a conversation node?
                (p.transform.position - this.transform.position)// is in range?
                .magnitude <= interactionRadius;
            });
            if (target != null)
            {
                // Kick off the dialogue at this node.
                FindObjectOfType<DialogueRunner>().StartDialogue(target.talkToNode);
                // reenabling the input on the dialogue
                dialogueInput.enabled = true;
            }
        }
    }
}
