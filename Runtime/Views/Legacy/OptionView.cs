/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Legacy
{
    [Obsolete]
    public class OptionView : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] TextMeshProUGUI? text;
        [SerializeField] bool showCharacterName = false;

        public Action<DialogueOption>? OnOptionSelected;
        public MarkupPalette? palette;

        DialogueOption? _option;

        bool hasSubmittedOptionSelection = false;

        public DialogueOption? Option
        {
            get => _option;

            set
            {
                _option = value;

                hasSubmittedOptionSelection = false;

                if (value == null)
                {
                    if (text != null)
                    {
                        text.text = "";
                    }
                    return;
                }

                // When we're given an Option, use its text and update our
                // interactibility.
                Markup.MarkupParseResult line;
                if (showCharacterName)
                {
                    line = value.Line.Text;
                }
                else
                {
                    line = value.Line.TextWithoutCharacterName;
                }

                if (text != null)
                {
                    if (palette != null)
                    {
                        text.text = LineView.PaletteMarkedUpText(line, palette, false);
                    }
                    else
                    {
                        text.text = line.Text;
                    }
                }

                interactable = value.IsAvailable;
            }
        }

        // If we receive a submit or click event, invoke our "we just selected
        // this option" handler.
        public void OnSubmit(BaseEventData eventData)
        {
            InvokeOptionSelected();
        }

        public void InvokeOptionSelected()
        {
            // turns out that Selectable subclasses aren't intrinsically
            // interactive/non-interactive based on their canvasgroup, you still
            // need to check at the moment of interaction
            if (!IsInteractable())
            {
                return;
            }

            if (Option == null)
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
                OnOptionSelected?.Invoke(Option);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            InvokeOptionSelected();
        }

        // If we mouse-over, we're telling the UI system that this element is
        // the currently 'selected' (i.e. focused) element. 
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.Select();
        }
    }
}
