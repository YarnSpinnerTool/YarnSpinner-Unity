/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity.Attributes;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/


#nullable enable

namespace Yarn.Unity
{
    public class LinePresenterButtonHandler : ActionMarkupHandler
    {
        [MustNotBeNull] [SerializeField] Button continueButton;

        [MustNotBeNullWhen(nameof(continueButton), "A " + nameof(DialogueRunner) + " must be provided for the continue button to work.")]
        [SerializeField] DialogueRunner? dialogueRunner;

        void Start()
        {
            continueButton.interactable = false;
            continueButton.enabled = false;
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
        {
            // enable the button
            continueButton.interactable = true;
            continueButton.enabled = true;

            continueButton.onClick.AddListener(() =>
            {
                if (dialogueRunner == null)
                {
                    Debug.LogWarning($"Continue button was clicked, but {nameof(dialogueRunner)} is null!", this);
                    return;
                }

                dialogueRunner.RequestNextLine();
            });
        }

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            return;
        }

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            return YarnTask.CompletedTask;
        }

        public override void OnLineDisplayComplete()
        {
            return;
        }

        public override void OnLineWillDismiss()
        {
            // disable interaction
            continueButton.onClick.RemoveAllListeners();
            continueButton.interactable = false;
            continueButton.enabled = false;
        }
    }
}