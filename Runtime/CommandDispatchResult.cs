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
    /// Represents the result of attempting to locate and call a command.
    /// </summary>
    /// <seealso cref="DispatchCommandToGameObject(Command, Action)"/>
    /// <seealso cref="DispatchCommandToRegisteredHandlers(Command, Action)"/>
    internal struct CommandDispatchResult
    {

        internal enum StatusType
        {

            SucceededAsync,

            SucceededSync,

            NoTargetFound,

            TargetMissingComponent,

            InvalidParameterCount,

            /// <summary>
            /// The command could not be found.
            /// </summary>
            CommandUnknown,

            /// <summary>
            /// The command was located and successfully called.
            /// </summary>
            [Obsolete("Use a more specific enum case", true)]
            Success,

            /// <summary>
            /// The command was located, but failed to be called.
            /// </summary>
            [Obsolete("Use a more specific enum case", true)]
            Failed,
        };

        internal StatusType Status;

        internal string Message;

        internal bool IsSuccess => this.Status == StatusType.SucceededAsync || this.Status == StatusType.SucceededSync;
    }
}
