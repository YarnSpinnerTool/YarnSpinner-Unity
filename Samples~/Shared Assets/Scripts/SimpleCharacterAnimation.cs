#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using System.Threading;
    using UnityEngine;

    public class SimpleCharacterAnimation : MonoBehaviour
    {
        [SerializeField] private Animator? animator;
        [SerializeField] private SimpleCharacterMovement? movement;
        [SerializeField] private string speedParameter = "Speed";

        [SerializeField] SerializableDictionary<string, string> facialExpressions = new();
        [SerializeField] string defaultFace = "";
        [SerializeField] string facialExpressionsLayer = "Face";
        private int facialExpressionsLayerID = 0;

        [Header("Body Tilt")]
        [SerializeField] string sideTiltParameter = "Side Tilt";
        [SerializeField] string forwardTiltParameter = "Forward Tilt";
        [SerializeField] string turnParameter = "Turn";


        [Header("Blinking")]
        [SerializeField] float meanBlinkTime = 2f;
        [SerializeField] float blinkTimeVariance = 0.5f;
        [SerializeField] string blinkTriggerName = "Blink";

        private float timeUntilNextBlink = 0f;

        [Header("Debug")]
        [SerializeField] bool overrideTilt = false;
        [Range(-1, 1)]
        [SerializeField] float turn = 0f;
        [Range(-1, 1)]
        [SerializeField] float sideTilt = 0f;
        [Range(-1, 1)]
        [SerializeField] float forwardTilt = 0f;

        private float SideTilt
        {
            get => (animator != null) ? animator.GetFloat(sideTiltParameter) : 0;
            set { if (animator != null) { animator.SetFloat(sideTiltParameter, value); } }
        }
        private float ForwardTilt
        {
            get => (animator != null) ? animator.GetFloat(forwardTiltParameter) : 0;
            set { if (animator != null) { animator.SetFloat(forwardTiltParameter, value); } }
        }
        private float Turn
        {
            get => (animator != null) ? animator.GetFloat(turnParameter) : 0;
            set { if (animator != null) { animator.SetFloat(turnParameter, value); } }
        }

        public void OnValidate()
        {
            animator = GetComponentInChildren<Animator>();
            movement = GetComponentInChildren<SimpleCharacterMovement>();
        }

        protected void Awake()
        {
            if (animator != null)
            {
                facialExpressionsLayerID = animator.GetLayerIndex(facialExpressionsLayer);
            }

            timeUntilNextBlink = GetNextBlinkTime();
        }

        CancellationTokenSource? sideTiltCancellation;
        CancellationTokenSource? forwardTiltCancellation;
        CancellationTokenSource? turnCancellation;

        private static float EaseInOutQuad(float x)
        {
            return x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2;
        }

        private async YarnTask RunTiltAnimation(float time, System.Action<float> action, CancellationToken cancellationToken)
        {
            if (time <= 0)
            {
                action(1);
                return;
            }

            action(0);
            var startTime = Time.time;
            while (Time.time < (startTime + time))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var t = Mathf.Clamp01((Time.time - startTime) / time);
                var easedT = EaseInOutQuad(t);

                action(easedT);
                await YarnTask.Yield();
            }
            action(1);
        }

        [YarnCommand("tilt-forward")]
        public YarnTask TiltForward(float destination, float time = 0f, bool wait = false)
        {
            // Cancel any existing animation
            if (forwardTiltCancellation != null)
            {
                forwardTiltCancellation.Cancel();
            }

            forwardTiltCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);

            var startPos = ForwardTilt;
            var task = RunTiltAnimation(time, (t) => ForwardTilt = Mathf.Lerp(startPos, destination, t), forwardTiltCancellation.Token);

            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("tilt-side")]
        public YarnTask TiltSide(float destination, float time = 0f, bool wait = false)
        {
            // Cancel any existing animation
            if (sideTiltCancellation != null)
            {
                sideTiltCancellation.Cancel();
            }

            sideTiltCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);

            var startPos = SideTilt;
            var task = RunTiltAnimation(time, (t) => SideTilt = Mathf.Lerp(startPos, destination, t), sideTiltCancellation.Token);

            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("turn")]
        public YarnTask TurnCharacter(float destination, float time = 0f, bool wait = false)
        {
            // Cancel any existing animation
            if (turnCancellation != null)
            {
                turnCancellation.Cancel();
            }

            turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);

            var startPos = Turn;
            var task = RunTiltAnimation(time, (t) => Turn = Mathf.Lerp(startPos, destination, t), turnCancellation.Token);

            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("set-animator-bool")]
        public void SetAnimatorBool(string parameterName, bool value)
        {
            if (animator == null)
            {
                Debug.LogError($"Can't set parameter {parameterName}: animator is not set");
                return;
            }
            animator.SetBool(parameterName, value);
        }

        [YarnCommand("play-anim")]
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
                return WaitForAnimation(animator, stateHash, layerIndex);
            }
            else
            {
                return YarnTask.CompletedTask;
            }

            static async YarnTask WaitForAnimation(Animator animator, int stateNameHash, int layerIndex)
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

        public void Update()
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

            if (movement != null && !string.IsNullOrEmpty(speedParameter))
            {
                animator.SetFloat(speedParameter, movement.CurrentSpeedFactor);
            }

            if (overrideTilt)
            {
                ForwardTilt = forwardTilt;
                SideTilt = sideTilt;
                Turn = turn;
            }
        }

        private float GetNextBlinkTime()
        {
            return meanBlinkTime + Mathf.Lerp(-blinkTimeVariance, blinkTimeVariance, UnityEngine.Random.value);
        }
    }
}