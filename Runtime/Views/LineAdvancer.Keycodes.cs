
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity.LineAdvancerInput
{
    [RequireComponent(typeof(LineAdvancer))]
    public class KeyCodes : MonoBehaviour, ILineAdvancerInput
    {
        [SerializeField] LineAdvancer? lineAdvancer;

        public LineAdvancer? LineAdvancer { get => lineAdvancer; set => lineAdvancer = value; }


        /// <summary>
        /// The <see cref="KeyCode"/> that triggers a request to advance to the
        /// next piece of content.
        /// </summary>
        public KeyCode hurryUpLineKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// current line.
        /// </summary>
        [ShowIf(nameof(ShowNextLine))]
        public KeyCode nextLineKeyCode = KeyCode.Escape;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to hurry up presenting options
        /// </summary>
        public KeyCode hurryUpOptionsKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>
        public KeyCode cancelDialogueKeyCode = KeyCode.None;

        private bool ShowNextLine => lineAdvancer != null && lineAdvancer.SeparateHurryUpAndAdvanceControls;

        protected void Update()
        {
            if (lineAdvancer == null)
            {
                return;
            }
            if (InputSystemAvailability.GetKeyDown(hurryUpLineKeyCode)) { lineAdvancer.OnInputHurryUpLines(); }
            if (InputSystemAvailability.GetKeyDown(nextLineKeyCode)) { lineAdvancer.OnInputNextContent(); }
            if (InputSystemAvailability.GetKeyDown(hurryUpOptionsKeyCode)) { lineAdvancer.OnInputHurryUpOptions(); }
            if (InputSystemAvailability.GetKeyDown(cancelDialogueKeyCode)) { lineAdvancer.OnInputCancelDialogue(); }
        }

        public void OnDialogueStarted() { }

        public void OnDialogueComplete() { }
    }
}
