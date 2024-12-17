#nullable enable

namespace Yarn.Unity.Samples
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    public class SimpleCharacterInteraction : MonoBehaviour
    {
        [SerializeField] float interactionRadius = 1f;
        [SerializeField] Vector3 offset = Vector3.zero;

        private List<Interactable> interactables = new();

        private Interactable? currentInteractable = null;

        private bool allowInteraction = true;

        [SerializeField] UnityEvent<Interactable>? onInteracted;

        public void SetInteractionAllowed(bool interactionAllowed) => allowInteraction = interactionAllowed;

        public void UpdateKnownInteractables()
        {
            interactables.Clear();

            interactables.AddRange(FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        public void Start()
        {
            UpdateKnownInteractables();
            allowInteraction = true;
        }

        protected void Update()
        {
            var previousInteractable = currentInteractable;

            (float Distance, Interactable? Interactable) nearest = (float.PositiveInfinity, null);

            if (allowInteraction)
            {
                for (int i = 0; i < interactables.Count; i++)
                {
                    var interactable = interactables[i];
                    var distance = Vector3.Distance(transform.TransformPoint(offset), interactable.transform.position);
                    if (distance > interactionRadius)
                    {
                        continue;
                    }
                    if (distance < nearest.Distance)
                    {
                        nearest = (distance, interactable);
                    }
                }
            }

            if (previousInteractable != nearest.Interactable)
            {
                if (previousInteractable != null) { previousInteractable.IsCurrent = false; }
                if (nearest.Interactable != null) { nearest.Interactable.IsCurrent = true; }
                currentInteractable = nearest.Interactable;
            }

            if (Input.GetButtonDown("Jump") && currentInteractable != null)
            {
                currentInteractable.Interact(this);
                onInteracted?.Invoke(currentInteractable);
            }
        }

        protected void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireSphere(offset, interactionRadius);
        }
    }
}