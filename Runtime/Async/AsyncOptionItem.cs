/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.EventSystems;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#nullable enable

#if USE_UNITASK
using YarnOptionCompletionSource = Cysharp.Threading.Tasks.UniTaskCompletionSource<Yarn.Unity.DialogueOption?>;
#else
using YarnOptionCompletionSource = System.Threading.Tasks.TaskCompletionSource<Yarn.Unity.DialogueOption?>;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// An on-screen item view that displays a single option, and signals if the
    /// user selects it.
    /// </summary>
    public class AsyncOptionItem : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        [MustNotBeNull]
        [SerializeField] TextMeshProUGUI? text;

        /// <summary>
        /// An completion source that will become completed when the user
        /// selects this view.
        /// </summary>
        public YarnOptionCompletionSource? onOptionSelected;

        private bool hasSubmittedOptionSelection = false;

        private DialogueOption? _option;

        /// <summary>
        /// Gets or sets the <see cref="Option"/> associated with this option
        /// view.
        /// </summary>
        public DialogueOption? Option
        {
            get => _option;

            set
            {
                _option = value;

                hasSubmittedOptionSelection = false;

                // When we're given an Option, use its text and update our
                // interactibility.
                if (value != null)
                {
                    if (text != null)
                    {
                        text.text = value.Line.TextWithoutCharacterName.Text;
                    }
                    interactable = value.IsAvailable;
                }
                else
                {
                    if (text != null)
                    {
                        text.text = string.Empty;
                    }
                    interactable = false;
                }
            }
        }

        // If we receive a submit or click event, invoke our "we just selected
        // this option" handler.

        /// <summary>
        /// Selects <see cref="Option"/> when this option item receives a submit
        /// event.
        /// </summary>
        /// <param name="eventData">Data related to the submit event.</param>
        public void OnSubmit(BaseEventData eventData)
        {
            InvokeOptionSelected();
        }

        /// <summary>
        /// Sets the result of <see cref="onOptionSelected"/> to the the current
        /// <see cref="Option"/>, if this option view is interactable.
        /// </summary>
        public void InvokeOptionSelected()
        {
            // turns out that Selectable subclasses aren't intrinsically
            // interactive/non-interactive based on their canvasgroup, you still
            // need to check at the moment of interaction
            if (!IsInteractable())
            {
                return;
            }

            // We only want to invoke this once, because it's an error to submit
            // an option when the Dialogue Runner isn't expecting it. To prevent
            // this, we'll only invoke this if the flag hasn't been cleared
            // already.
            if (hasSubmittedOptionSelection == false)
            {
                hasSubmittedOptionSelection = true;

                onOptionSelected?.TrySetResult(this.Option);
            }
        }

        /// <summary>
        /// Selects <see cref="Option"/> when this option item receives a
        /// pointer click event.
        /// </summary>
        /// <param name="eventData">Data related to the click event.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            InvokeOptionSelected();
        }

        /// <summary>
        /// Selects the option item when a pointer enters the view.
        /// </summary>
        /// <param name="eventData">Data related to the pointer enter
        /// event.</param>
        public override void OnPointerEnter(PointerEventData eventData)
        {
            // If we mouse-over, we're telling the UI system that this element is
            // the currently 'selected' (i.e. focused) element. 
            base.Select();
        }
    }
}
