/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
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
