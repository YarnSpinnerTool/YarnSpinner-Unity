#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using System.Threading;

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
                return;
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
            if (interactable.InteractorShouldTurnToFaceWhenInteracted)
            {
                TurnToPosition(interactable.transform.position).Forget();
            }
        }
        public async YarnTask TurnToPosition(Vector3 position)
        {
            float angle;
            do
            {
                var positionAtSameY = position;
                positionAtSameY.y = transform.position.y;
                var lookDirection = positionAtSameY - transform.position;
                var desiredOrientation = Quaternion.LookRotation(lookDirection);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredOrientation, this.turnSpeed * Time.deltaTime);
                angle = Quaternion.Angle(transform.rotation, desiredOrientation);

                await YarnTask.Yield();
            }
            while (!Application.exitCancellationToken.IsCancellationRequested && !allowMovement && angle > 0.1f);
        }

        public async YarnTask MoveTo(Vector3 position, CancellationToken cancellationToken)
        {
            await TurnToPosition(position);

            do
            {
                transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
                this.CurrentSpeedFactor = 1;

                await YarnTask.Yield();

            } while (Vector3.Distance(transform.position, position) > 0.05f && !cancellationToken.IsCancellationRequested);
            
            this.CurrentSpeedFactor = 0;
        }
    }
}