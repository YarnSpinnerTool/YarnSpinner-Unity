#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;

    public class SimpleCharacterMovement : MonoBehaviour
    {
        [SerializeField] float speed;
        [SerializeField] float gravity = 10;
        [SerializeField] float turnSpeed;

        [SerializeField] float acceleration = 0.5f;
        [SerializeField] float deceleration = 0.1f;

        public float CurrentSpeedFactor { get; private set; } = 0f;

        private float lastFrameSpeed = 0f;
        private float lastFrameSpeedChange = 0f;
        private Vector2 lastInput = Vector2.zero;

        private CharacterController? characterController;

        private bool allowMovement = true;

        public void SetMovementAllowed(bool movementAllowed) => allowMovement = movementAllowed;

        protected void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        protected void Update()
        {
            Vector2 input = new(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            if (!allowMovement)
            {
                input = Vector2.zero;
            }

            float rawSpeed;

            if (input.magnitude < 0.001)
            {
                input = lastInput;
                rawSpeed = 0f;
            }
            else
            {
                rawSpeed = Mathf.Clamp01(input.magnitude) * speed;
                lastInput = input;
            }

            var dampingTime = (rawSpeed > lastFrameSpeed) ? acceleration : deceleration;

            var dampedSpeed = Mathf.SmoothDamp(lastFrameSpeed, rawSpeed, ref lastFrameSpeedChange, dampingTime);
            lastFrameSpeed = dampedSpeed;

            var movement = new Vector3(
                input.x,
                0,
                input.y
            );

            if (allowMovement && movement.magnitude > 0)
            {
                var from = Quaternion.LookRotation(transform.forward);
                var to = Quaternion.LookRotation(movement.normalized);

                transform.rotation = Quaternion.RotateTowards(from, to, turnSpeed * Time.deltaTime);
            }

            movement = movement.normalized * dampedSpeed;
            movement.y = -gravity;

            if (characterController != null)
            {
                characterController.Move(movement * Time.deltaTime);
            }

            CurrentSpeedFactor = Mathf.Clamp01(dampedSpeed / speed);
        }

        public void OnInteracted(Interactable interactable)
        {
            async YarnTask TurnToInteractor()
            {
                float angle;
                do
                {
                    var positionAtSameY = interactable.transform.position;
                    positionAtSameY.y = transform.position.y;
                    var lookDirection = positionAtSameY - transform.position;
                    var desiredOrientation = Quaternion.LookRotation(lookDirection);

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredOrientation, this.turnSpeed * Time.deltaTime);
                    angle = Quaternion.Angle(transform.rotation, desiredOrientation);
                    Debug.Log($"{desiredOrientation.eulerAngles}; My angle: " + angle);

                    await YarnTask.Yield();
                } while (!Application.exitCancellationToken.IsCancellationRequested && !allowMovement && angle > 0.1f);

                Debug.Log($"Turn complete!");
            }

            if (interactable.InteractorShouldTurnToFaceWhenInteracted)
            {
                TurnToInteractor().Forget();
            }
        }
    }
}