/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

#nullable enable

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// A subclass of <see cref="DialogueViewBase"/> that updates the colour of
    /// a <see cref="TMPro.TMP_Text"/> object based on the character speaking a
    /// line. names.
    /// </summary>
    /// <remarks>
    /// <para>This class uses the `character` attribute on lines that it
    /// receives to determine its content. When the view's <see
    /// cref="RunLineAsync"/> method is called with a line whose <see
    /// cref="LocalizedLine.Text"/> contains a `character` attribute, the text
    /// views have their <see cref="TMPro.TMP_Text.color"/> property updated
    /// based on the colours configured in the Inspector.
    /// </para>
    ///
    /// <para>This view does not present any options or handle commands. It's
    /// intended to be used alongside other subclasses of <see
    /// cref="AsyncDialogueViewBase"/>.</para>
    /// </remarks>
    public class CharacterColorView : Yarn.Unity.AsyncDialogueViewBase
    {
        /// <summary>
        /// Associates a named character with a colour to use in a <see
        /// cref="CharacterColorView"/>.
        /// </summary>
        [Serializable]
        public class CharacterColorData
        {
            /// <summary>
            /// The name of a speaking character.
            /// </summary>
            public string? characterName;

            /// <summary>
            /// The text colour associated with this character.
            /// </summary>
            public Color displayColor = Color.white;
        }

        /// <summary>
        /// The default colour to use for the text views if a suitable character
        /// name cannot be found.
        /// </summary>
        [SerializeField] Color defaultColor = Color.white;

        /// <summary>
        /// The list of objects that map character names to colours.
        /// </summary>
        [SerializeField] CharacterColorData[] colorData;

        /// <summary>
        /// The text views to update the colour of when a line is run.
        /// </summary>
        [SerializeField] List<TextMeshProUGUI> lineTexts = new List<TextMeshProUGUI>();

        /// <summary>
        /// Updates the text colour of <see cref="lineTexts"/> based on the
        /// character name of <paramref name="line"/>, if any.
        /// </summary>
        /// <remarks>If the line doesn't have a character name, or if the
        /// character name is not found in <see cref="colorData"/>, <see
        /// cref="defaultColor"/> is used.</remarks>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync" path="/param"
        /// />
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync"
        /// path="/returns" />
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            var characterName = line.CharacterName;

            Color colorToUse = defaultColor;

            if (string.IsNullOrEmpty(characterName) == false)
            {
                foreach (var color in colorData)
                {
                    if (color.characterName?.Equals(characterName, StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        colorToUse = color.displayColor;
                        break;
                    }
                }
            }

            foreach (var text in lineTexts)
            {
                text.color = colorToUse;
            }

            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return DialogueRunner.NoOptionSelected;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }
    }
}
