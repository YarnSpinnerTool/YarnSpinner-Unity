/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity.Attributes;

#if USE_TMP
using TMPro;
#else
    using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity
{
    [System.Serializable]
    internal struct InternalAppearance
    {
        [SerializeField] internal Sprite sprite;
        [SerializeField] internal Color colour;
    }

    public sealed class OptionItem : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        [MustNotBeNull, SerializeField] TextMeshProUGUI? text;
        [SerializeField] UnityEngine.UI.Image? selectionImage;

        [Group("Appearance"), SerializeField] InternalAppearance normal;
        [Group("Appearance"), SerializeField] InternalAppearance selected;
        [Group("Appearance"), SerializeField] InternalAppearance disabled;

        [Group("Appearance"), SerializeField] bool disabledStrikeThrough = true;

        public YarnTaskCompletionSource<DialogueOption?>? OnOptionSelected;
        public System.Threading.CancellationToken completionToken;

        private bool hasSubmittedOptionSelection = false;

        private DialogueOption? _option;
        public DialogueOption Option
        {
            get
            {
                if (_option == null)
                {
                    throw new System.NullReferenceException("Option has not been set on the option item");
                }
                return _option;
            }

            set
            {
                _option = value;

                hasSubmittedOptionSelection = false;

                // When we're given an Option, use its text and update our
                // interactibility.
                string line = value.Line.TextWithoutCharacterName.Text;
                if (disabledStrikeThrough && !value.IsAvailable)
                {
                    line = $"<s>{value.Line.TextWithoutCharacterName.Text}</s>";
                }

                if (text == null)
                {
                    Debug.LogWarning($"The {nameof(text)} is null, is it not connected in the inspector?", this);
                    return;
                }

                text.text = line;
                interactable = value.IsAvailable;

                // we want to apply the default styling to the option item when they are given an option
                ApplyStyle(normal);
            }
        }

        private void ApplyStyle(InternalAppearance style)
        {
            Color newColour = style.colour;
            Sprite newSprite = style.sprite;
            if (!Option.IsAvailable)
            {
                newColour = disabled.colour;
                newSprite = disabled.sprite;
            }

            if (text == null)
            {
                Debug.LogWarning($"The {nameof(text)} is null, is it not connected in the inspector?", this);
                return;
            }

            text.color = newColour;

            if (selectionImage != null)
            {
                selectionImage.color = newColour;
                if (newSprite != null)
                {
                    selectionImage.sprite = newSprite;
                    selectionImage.gameObject.SetActive(true);
                }
                else
                {
                    selectionImage.gameObject.SetActive(false);
                }
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            ApplyStyle(selected);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);

            ApplyStyle(normal);
        }

        new public bool IsHighlighted
        {
            get
            {
                return EventSystem.current.currentSelectedGameObject == this.gameObject;
            }
        }

        // If we receive a submit or click event, invoke our "we just selected this option" handler.
        public void OnSubmit(BaseEventData eventData)
        {
            InvokeOptionSelected();
        }

        public void InvokeOptionSelected()
        {
            // turns out that Selectable subclasses aren't intrinsically interactive/non-interactive
            // based on their canvasgroup, you still need to check at the moment of interaction
            if (!IsInteractable())
            {
                return;
            }

            // We only want to invoke this once, because it's an error to
            // submit an option when the Dialogue Runner isn't expecting it. To
            // prevent this, we'll only invoke this if the flag hasn't been cleared already.
            if (hasSubmittedOptionSelection == false && !completionToken.IsCancellationRequested)
            {
                hasSubmittedOptionSelection = true;
                OnOptionSelected?.TrySetResult(this.Option);
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
