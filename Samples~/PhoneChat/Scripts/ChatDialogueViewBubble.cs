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
    /// <summary>
    /// A UI element that displays the text of a dialogue line in a style
    /// resembling messaging apps, and can simulate a 'typing' indicator.
    /// </summary>
    public class ChatDialogueViewBubble : MonoBehaviour
    {
        /// <summary>
        /// The typing indicator.
        /// </summary>
        [SerializeField] GameObject? typingIndicator;

        /// <summary>
        /// The text view that shows the contents of the message.
        /// </summary>
        private TMP_Text? TextView => GetComponentInChildren<TMP_Text>();

        /// <summary>
        /// Gets a value indicating whether this bubble prefab has a typing
        /// indicator.
        /// </summary>
        public bool HasIndicator => typingIndicator != null;

        /// <summary>
        /// Shows the typing indicator if present, and clears the text view.
        /// </summary>
        public void ShowTyping()
        {
            if (typingIndicator != null)
            {
                typingIndicator.SetActive(true);
            }
            if (TextView != null)
            {
                TextView.text = string.Empty;
            }
        }

        /// <summary>
        /// Shows the specified text in the text view, and hides the typing
        /// indicator.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public void ShowText(string text)
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