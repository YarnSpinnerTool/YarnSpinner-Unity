/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    /// <summary>
    /// A button that displays a dialogue option in the Phone Chat sample.
    /// </summary>
    public class ChatDialogueViewOptionsButton : MonoBehaviour
    {
        /// <summary>
        /// The text view that shows the text of the option.
        /// </summary>
        private TMP_Text? TextView => GetComponentInChildren<TMP_Text>();

        /// <summary>
        /// Gets or sets the text shown in the option button.
        /// </summary>
        public string Text
        {
            get => (TextView != null) ? TextView.text : string.Empty;
            set { if (TextView != null) { TextView.text = value; } }
        }

        /// <summary>
        /// The delegate to run when the button is clicked.
        /// </summary>
        public Action? OnClick { get; internal set; }

        /// <summary>
        /// Called by the <see cref="UnityEngine.UI.Button"/> component when
        /// clicked. 
        /// </summary>
        public void OnClicked()
        {
            OnClick?.Invoke();
        }
    }
}