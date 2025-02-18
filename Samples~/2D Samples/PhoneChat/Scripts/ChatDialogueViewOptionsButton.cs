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
    public class ChatDialogueViewOptionsButton : MonoBehaviour
    {
        private TMP_Text? TextView => GetComponentInChildren<TMP_Text>();

        public string Text
        {
            get => (TextView != null) ? TextView.text : string.Empty;
            set { if (TextView != null) { TextView.text = value; } }
        }

        public Func<bool>? OnClick { get; internal set; }
        public void OnClicked()
        {
            OnClick?.Invoke();
        }
    }
}