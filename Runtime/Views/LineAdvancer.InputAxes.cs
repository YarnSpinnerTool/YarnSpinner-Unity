
using UnityEngine;
using Yarn.Unity.Attributes;

namespace Yarn.Unity.LineAdvancerInput
{
    [RequireComponent(typeof(LineAdvancer))]
    public class LegacyInputAxes : MonoBehaviour, ILineAdvancerInput
    {
        [MessageBox(nameof(GetAvailabilityMessage))]
        [SerializeField] LineAdvancer lineAdvancer;

#pragma warning disable CS0162
        public MessageBoxAttribute.Message GetAvailabilityMessage()
        {
            if (!InputSystemAvailability.enableLegacyInput)
            {
                return MessageBoxAttribute.Warning("The legacy Input Manager system is not enabled.");
            }
            return MessageBoxAttribute.NoMessage;
        }
#pragma warning restore CS0162

        public LineAdvancer LineAdvancer { get => lineAdvancer; set => lineAdvancer = value; }

        /// <summary>
        /// The legacy Input Axis that triggers a request to advance to the next
        /// piece of content.
        /// </summary>
        public string hurryUpLineAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// current line.
        /// </summary>
        [ShowIf(nameof(ShowNextLine))]
        public string nextLineAxis = "Cancel";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to hurry up presenting the current options
        /// </summary>
        public string hurryUpOptionsAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>
        public string cancelDialogueAxis = "";

        public void OnDialogueStarted() { }

        public void OnDialogueComplete() { }

        private bool ShowNextLine => lineAdvancer != null && lineAdvancer.SeparateHurryUpAndAdvanceControls;

        protected void Update()
        {
            if (lineAdvancer == null)
            {
                return;
            }

            if (InputSystemAvailability.GetButtonDown(hurryUpLineAxis)) { lineAdvancer.OnInputHurryUpLines(); }
            if (InputSystemAvailability.GetButtonDown(nextLineAxis)) { lineAdvancer.OnInputNextContent(); }
            if (InputSystemAvailability.GetButtonDown(hurryUpOptionsAxis)) { lineAdvancer.OnInputHurryUpOptions(); }
            if (InputSystemAvailability.GetButtonDown(cancelDialogueAxis)) { lineAdvancer.OnInputCancelDialogue(); }
        }
    }
}
