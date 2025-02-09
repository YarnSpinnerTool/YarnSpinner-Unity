#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using System.Threading;
    using Yarn.Unity.Attributes;
    using System.Collections.Generic;
    using UnityEngine.Events;

    public class SimpleCharacter : MonoBehaviour
    {
        public enum CharacterMode
        {
            PlayerControlledMovement,
            PathMovement,
            Interact,
        }

        public CharacterMode Mode { get; private set; }

        public bool CanInteract => Mode == CharacterMode.PlayerControlledMovement;

        [SerializeField] bool isPlayerControlled;

        #region Movement Variables

        [Group("Movement")]
        [SerializeField] float speed;
        [Group("Movement")]
        [SerializeField] float gravity = 10;
        [Group("Movement")]
        [SerializeField] float turnSpeed;

        [Group("Movement")]
        [SerializeField] float acceleration = 0.5f;
        [Group("Movement")]
        [SerializeField] float deceleration = 0.1f;
        [Group("Movement")]
        public Transform? lookTarget;

        private Quaternion targetRotation;

        public float CurrentSpeedFactor { get; private set; } = 0f;

        private float lastFrameSpeed = 0f;
        private float lastFrameSpeedChange = 0f;
        private Vector3 lastFrameWorldPosition;
        private Vector2 lastInput = Vector2.zero;

        private CharacterController? characterController;


        #endregion


        #region Animation Variables
        [Group("Animation")]
        [SerializeField] private Animator? animator;
        [Group("Animation")]
        [AnimationParameter(nameof(animator), AnimatorControllerParameterType.Float)]
        [SerializeField] private string speedParameter = "Speed";

        [Group("Animation")]
        [SerializeField] SerializableDictionary<string, string> facialExpressions = new();
        [Group("Animation")]
        [SerializeField] string defaultFace = "";
        [Group("Animation")]
        [SerializeField] string facialExpressionsLayer = "Face";
        private int facialExpressionsLayerID = 0;

        [Group("Animation")]
        [Header("Body Tilt")]
        [AnimationParameter(nameof(animator), AnimatorControllerParameterType.Float)]
        [SerializeField] string sideTiltParameter = "Side Tilt";
        [Group("Animation")]
        [AnimationParameter(nameof(animator), AnimatorControllerParameterType.Float)]
        [SerializeField] string forwardTiltParameter = "Forward Tilt";
        [Group("Animation")]
        [AnimationParameter(nameof(animator), AnimatorControllerParameterType.Float)]
        [SerializeField] string turnParameter = "Turn";


        [Group("Animation")]
        [Header("Blinking")]
        [SerializeField] float meanBlinkTime = 2f;
        [Group("Animation")]
        [SerializeField] float blinkTimeVariance = 0.5f;
        [Group("Animation")]
        [AnimationParameter(nameof(animator), AnimatorControllerParameterType.Trigger)]
        [SerializeField] string blinkTriggerName = "Blink";

        private float timeUntilNextBlink = 0f;
        private Dictionary<int, CancellationTokenSource> activeAnimationLerps = new();

        #endregion


        #region Interaction Variables
        [Group("Interaction")]
        [ShowIf(nameof(isPlayerControlled))]
        [SerializeField] float interactionRadius = 1f;
        [Group("Interaction")]
        [ShowIf(nameof(isPlayerControlled))]
        [SerializeField] Vector3 offset = Vector3.zero;

        [Group("Interaction")]
        [ShowIf(nameof(isPlayerControlled))]
        [SerializeField] UnityEvent<Interactable>? onInteracting;

        private List<Interactable> interactables = new();

        private Interactable? currentInteractable = null;

        #endregion



        #region Animation Commands

        [YarnCommand("tilt_forward")]
        public YarnTask TiltForward(float destination, float time = 0f, bool wait = false)
        {
            var task = TweenAnimationParameter(forwardTiltParameter, destination, time, EasingFunctions.InOutQuad, destroyCancellationToken);
            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("tilt_side")]
        public YarnTask TiltSide(float destination, float time = 0f, bool wait = false)
        {
            var task = TweenAnimationParameter(sideTiltParameter, destination, time, EasingFunctions.InOutQuad, destroyCancellationToken);
            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("turn")]
        public YarnTask TurnCharacter(float destination, float time = 0f, bool wait = false)
        {
            var task = TweenAnimationParameter(turnParameter, destination, time, EasingFunctions.InOutQuad, destroyCancellationToken);
            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("set_animator_bool")]
        public void SetAnimatorBool(string parameterName, bool value)
        {
            if (animator == null)
            {
                Debug.LogError($"Can't set parameter {parameterName}: animator is not set");
                return;
            }
            animator.SetBool(parameterName, value);
        }

        [YarnCommand("play_animation")]
        public YarnTask PlayAnimation(string layerName, string stateName, bool wait = false)
        {
            if (animator == null)
            {
                Debug.LogError($"Can't play animation {stateName}: animator is not set");
                return YarnTask.CompletedTask;
            }

            var layerIndex = animator.GetLayerIndex(layerName);
            if (layerIndex == -1)
            {
                Debug.LogError($"Can't play animation {stateName}: no layer {layerName} found");
                return YarnTask.CompletedTask;
            }

            var stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(layerIndex, stateHash) == false)
            {
                Debug.LogError($"Can't play animation {stateName}: no state {stateName} found in layer {layerName}");
                return YarnTask.CompletedTask;
            }

            animator.Play(stateHash, layerIndex);

            if (wait)
            {
                return WaitUntilAnimationComplete(animator, stateHash, layerIndex);
            }
            else
            {
                return YarnTask.CompletedTask;
            }

            static async YarnTask WaitUntilAnimationComplete(Animator animator, int stateNameHash, int layerIndex)
            {
                AnimatorStateInfo stateInfo;

                // Wait until the animator starts playing this state
                do
                {
                    stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                    await YarnTask.Yield();
                } while (stateInfo.shortNameHash != stateNameHash);

                // Wait until the animator is no longer playing this state
                // or has reached the end of the state
                do
                {
                    stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                    await YarnTask.Yield();
                } while (stateInfo.shortNameHash == stateNameHash || stateInfo.normalizedTime >= 1);
            }
        }

        [YarnCommand("face")]
        public void SetFacialExpression(string name, float crossfadeTime = 0)
        {
            if (animator == null)
            {
                Debug.LogWarning($"{name} has no {nameof(Animator)}");
                return;
            }

            if (!facialExpressions.TryGetValue(name, out var stateName))
            {
                Debug.LogWarning($"{name} is not a valid facial expression (expected {string.Join(", ", facialExpressions.Keys)})");
                return;
            }

            if (crossfadeTime <= 0)
            {
                animator.Play(stateName, facialExpressionsLayerID);
            }
            else
            {
                animator.CrossFadeInFixedTime(stateName, crossfadeTime, facialExpressionsLayerID);
            }
        }

        #endregion

        #region Animation Logic

        protected void SetupAnimation()
        {
            characterController = GetComponent<CharacterController>();

            if (animator != null)
            {
                facialExpressionsLayerID = animator.GetLayerIndex(facialExpressionsLayer);
            }

            timeUntilNextBlink = GetNextBlinkTime();
        }

        private float GetNextBlinkTime()
        {
            return meanBlinkTime + Mathf.Lerp(-blinkTimeVariance, blinkTimeVariance, UnityEngine.Random.value);
        }

        public void UpdateAnimation()
        {

            if (animator == null)
            {
                return;
            }

            timeUntilNextBlink -= Time.deltaTime;

            if (timeUntilNextBlink <= 0 && !string.IsNullOrEmpty(blinkTriggerName))
            {
                animator.SetTrigger(blinkTriggerName);
                timeUntilNextBlink = GetNextBlinkTime();
            }

            animator.SetFloat(speedParameter, CurrentSpeedFactor);
        }

        private async YarnTask TweenAnimationParameter(string animationParameter, float to, float duration, System.Func<float, float> easingFunction, CancellationToken cancellationToken)
        {
            if (animator == null)
            {
                return;
            }

            var hash = Animator.StringToHash(animationParameter);
            var currentValue = animator.GetFloat(hash);

            // If a tween was already running for this parameter, cancel it now
            if (activeAnimationLerps.TryGetValue(hash, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }

            // Create and store a cancellation token source for this animation
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            activeAnimationLerps[hash] = cts;

            // Run the tween
            await Tweening.TweenValue(currentValue, to, duration, easingFunction, value => animator.SetFloat(hash, value), cts.Token);

            // Clean up
            activeAnimationLerps.Remove(hash);
        }
        #endregion

        #region Movement Logic


        private void SetupMovement()
        {
            targetRotation = transform.rotation;
            lastFrameWorldPosition = transform.position;
        }

        protected void UpdateMovement()
        {
            var currentLookDirection = targetRotation;

            if (lookTarget != null)
            {
                var lookDirectionOnSameY = lookTarget.position - transform.position;
                lookDirectionOnSameY.y = 0;
                currentLookDirection = Quaternion.LookRotation(lookDirectionOnSameY);
            }

            if (isPlayerControlled && Mode == CharacterMode.PlayerControlledMovement)
            {
                Vector2 input = new(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );

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

                if (movement.magnitude > 0)
                {
                    // If we're moving, update the direction we want to be looking
                    // at when we have no look target
                    targetRotation = Quaternion.LookRotation(movement.normalized);
                }

                movement = movement.normalized * dampedSpeed;
                movement.y = -gravity;

                if (characterController != null)
                {
                    characterController.Move(movement * Time.deltaTime);
                }

                CurrentSpeedFactor = Mathf.Clamp01(dampedSpeed / speed);
            }
            else if (Mode == CharacterMode.PathMovement)
            {
                var currentSpeed = (lastFrameWorldPosition - transform.position).magnitude / Time.deltaTime;
                CurrentSpeedFactor = Mathf.Clamp01(currentSpeed / speed);
            }
            else
            {
                CurrentSpeedFactor = 0;
            }

            // Rotate towards our current look direction
            transform.rotation = Quaternion.RotateTowards(
                Quaternion.LookRotation(transform.forward),
                currentLookDirection,
                turnSpeed * Time.deltaTime
            );

            lastFrameWorldPosition = transform.position;
        }

        public async YarnTask MoveTo(Vector3 position, CancellationToken cancellationToken)
        {
            if (Vector3.Distance(position, transform.position) <= 0.0001f)
            {
                // We're already at the position; nothing to do
                return;
            }
            // Look in the direction we're moving, not at any look target
            var lookDirection = position - transform.position;
            lookDirection.y = 0;
            targetRotation = Quaternion.LookRotation(lookDirection);

            var previousLookTarget = lookTarget;
            lookTarget = null;

            var previousMode = Mode;

            Mode = CharacterMode.PathMovement;

            do
            {
                transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
                this.CurrentSpeedFactor = 1;

                await YarnTask.Yield();

            } while (Vector3.Distance(transform.position, position) > 0.05f && !cancellationToken.IsCancellationRequested);

            lookTarget = previousLookTarget;

            Mode = previousMode;

            this.CurrentSpeedFactor = 0;
        }
        #endregion

        #region Interaction Logic

        public void SetupInteraction()
        {
            interactables.Clear();

            interactables.AddRange(FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        protected void UpdateInteraction()
        {
            if (isPlayerControlled == false)
            {
                // Only player-controlled characters can interact
                return;
            }

            if (!CanInteract)
            {
                // We can only interact if we're allowed to move around.
                return;
            }

            var previousInteractable = currentInteractable;

            (float Distance, Interactable? Interactable) nearest = (float.PositiveInfinity, null);

            for (int i = 0; i < interactables.Count; i++)
            {
                var interactable = interactables[i];

                if (interactable.gameObject == gameObject)
                {
                    // We can't interact with ourselves
                    continue;
                }

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

            if (previousInteractable != nearest.Interactable)
            {
                if (previousInteractable != null) { previousInteractable.IsCurrent = false; }
                if (nearest.Interactable != null) { nearest.Interactable.IsCurrent = true; }
                currentInteractable = nearest.Interactable;
            }

            if (Input.GetButtonDown("Jump") && currentInteractable != null)
            {
                async YarnTask RunInteraction(Interactable interactable, CancellationToken cancellationToken)
                {
                    var previousMode = Mode;
                    Mode = CharacterMode.Interact;

                    if (interactable.InteractorShouldTurnToFaceWhenInteracted)
                    {
                        lookTarget = interactable.transform;
                    }

                    interactable.IsCurrent = false;
                    currentInteractable = null;

                    onInteracting?.Invoke(interactable);
                    await interactable.Interact(gameObject);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (interactable.InteractorShouldTurnToFaceWhenInteracted)
                    {
                        lookTarget = null;
                    }

                    Mode = previousMode;
                }

                RunInteraction(currentInteractable, this.destroyCancellationToken).Forget();
            }
        }

        #endregion



        #region Core Logic

        protected void Awake()
        {
            Mode = CharacterMode.PlayerControlledMovement;

            SetupMovement();
            SetupAnimation();
            SetupInteraction();
        }

        protected void Update()
        {
            UpdateMovement();
            UpdateAnimation();
            UpdateInteraction();
        }

        protected void OnDrawGizmosSelected()
        {
            if (isPlayerControlled)
            {
                // Show interaction volume
                Gizmos.color = Color.yellow;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.DrawWireSphere(offset, interactionRadius);
            }
        }
        #endregion
    }
}
