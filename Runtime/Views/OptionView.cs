using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace Yarn.Unity
{
    public class OptionView : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] bool showCharacterName = false;

        public Action<DialogueOption> OnOptionSelected;

        DialogueOption _option;

        bool hasSubmittedOptionSelection = false;

        public DialogueOption Option
        {
            get => _option;

            set
            {
                _option = value;

                hasSubmittedOptionSelection = false;

                // When we're given an Option, use its text and update our
                // interactibility.
                if (showCharacterName)
                {
                    text.text = value.Line.Text.Text;
                }
                else
                {
                    text.text = value.Line.TextWithoutCharacterName.Text;
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
            // We only want to invoke this once, because it's an error to
            // submit an option when the Dialogue Runner isn't expecting it. To
            // prevent this, we'll only invoke this if the flag hasn't been cleared already.
            if (hasSubmittedOptionSelection == false)
            {
                OnOptionSelected.Invoke(Option);
                hasSubmittedOptionSelection = true;
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