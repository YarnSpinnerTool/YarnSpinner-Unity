/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using System;

namespace Yarn.Unity
{
    /// <summary>
    /// The presentation status of a <see cref="LocalizedLine"/>.
    /// </summary>
    public enum LineStatus
    {
        /// <summary>
        /// The line is in the process of being presented to the user, but
        /// has not yet finished appearing.
        /// </summary>
        /// <remarks>
        /// Lines in this state are in the process of being delivered; for
        /// example, visual animations may be running, and audio playback
        /// may be ongoing.
        /// </remarks>
        Presenting,
        /// <summary>
        /// The user has interrupted the delivery of this line, by calling
        /// <see cref="DialogueViewBase.ReadyForNextLine"/> before all line
        /// views finished delivering the line. All line views should
        /// finish delivering the line as quickly as possible, and then
        /// signal that the line has been <see cref="FinishedPresenting"/>.
        /// </summary>
        /// <remarks>
        /// For example, any animations should skip to the end, either
        /// immediately or very quickly, and any audio should stop or
        /// quickly fade out.
        /// </remarks>
        Interrupted,
        /// <summary>
        /// The line has finished being delivered to the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a line has finished being delivered, any animations in
        /// showing text and any audio playback should now be complete.
        /// </para>
        /// <para>
        /// A line remains in the <see cref="FinishedPresenting"/> state until a
        /// Dialogue View calls <see
        /// cref="DialogueViewBase.ReadyForNextLine"/>. At this point, the
        /// line will transition to the <see cref="Dismissed"/> state, and <see
        /// cref="DialogueViewBase.DismissLine(Action)"/> will be called on
        /// all views to dismiss the line.
        /// </para>
        /// </remarks>
        FinishedPresenting,
        /// <summary>
        /// The line is not being presented anymore in any way to the user.
        /// </summary>
        Dismissed
    }
}
