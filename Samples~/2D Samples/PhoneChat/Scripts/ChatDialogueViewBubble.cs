/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.UI;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    public class ChatDialogueViewBubble : MonoBehaviour
    {
        [SerializeField] GameObject? typingIndicator;
        private TMP_Text? TextView => GetComponentInChildren<TMP_Text>();

        public bool HasIndicator => typingIndicator != null;

        public void SetTyping(bool typing)
        {
            if (typingIndicator != null)
            {
                typingIndicator.SetActive(typing);
            }
            if (TextView != null)
            {
                TextView.text = string.Empty;
            }
        }


        public void SetText(string text)
        {
            if (typingIndicator != null)
            {
                typingIndicator.SetActive(false);
            }
            if (TextView != null)
            {
                TextView.text = text;
            }
        }
    }
}