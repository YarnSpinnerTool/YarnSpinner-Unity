/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Linq;
using UnityEngine;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Yarn.Unity.Tests
{
    public class EndToEndTestPlayer : MonoBehaviour
    {

        readonly Vector3 Up = new Vector3(-1, 0, 0);
        readonly Vector3 Right = new Vector3(0, 0, 1);

        public float speed = 5f;
        public float interactionRange = 5f;

        DialogueRunner dialogueRunner;

        public void Awake()
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        public void Update()
        {
            if (dialogueRunner.IsDialogueRunning)
            {
                return;
            }

            Vector3 movement = Vector2.zero;

            if (Input.GetKey(KeyCode.W))
            {
                movement += Up;
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement += -Up;
            }
            if (Input.GetKey(KeyCode.D))
            {
                movement += Right;
            }
            if (Input.GetKey(KeyCode.A))
            {
                movement += -Right;
            }
            if (movement.sqrMagnitude > 0)
            {
                movement = movement.normalized;
                movement *= Time.deltaTime * speed;
                transform.Translate(movement);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                var nearbyNPC = FindObjectsByType<EndToEndTestNPC>(FindObjectsSortMode.None)
                    .Select(n => (NPC: n, Distance: Vector3.Distance(n.transform.position, transform.position)))
                    .OrderBy(p => p.Distance)
                    .FirstOrDefault(p => p.Distance < interactionRange)
                    .NPC;

                if (nearbyNPC != null && string.IsNullOrEmpty(nearbyNPC.nodeName) == false)
                {
                    if (dialogueRunner.IsDialogueRunning == false)
                    {
                        dialogueRunner.StartDialogue(nearbyNPC.nodeName);
                    }
                }
            }
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
