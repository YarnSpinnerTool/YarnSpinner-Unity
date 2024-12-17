/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity
{
    public class DialogueOption
    {
        /// <summary>
        /// The ID of this dialogue option
        /// </summary>
        public int DialogueOptionID;

        /// <summary>
        /// The ID of the dialogue option's text
        /// </summary>
        public string TextID = "<unknown>";

        /// <summary>
        /// The line for this dialogue option
        /// </summary>
        public LocalizedLine Line = LocalizedLine.InvalidLine;

        /// <summary>
        /// Indicates whether this value should be presented as available
        /// or not.
        /// </summary>
        public bool IsAvailable;
    }
}
