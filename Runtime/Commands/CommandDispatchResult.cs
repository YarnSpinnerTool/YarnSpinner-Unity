/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;

#nullable enable

namespace Yarn.Unity
{
    internal enum StatusType
    {
        NoTargetFound,

        InvalidParameterCount,

        InvalidParameter,

        /// <summary>
        /// The action could not be found.
        /// </summary>
        ActionUnknown,

        InvalidReturn,
    }

    internal interface IDispatchResult { }
    internal struct ActionDispatchFailed: IDispatchResult
    {
        internal StatusType Status;

        internal ActionDispatchFailed(StatusType status)
        {
            Status = status;
        }
    }
    internal struct CommandDispatchSucceededSynchronously: IDispatchResult { }
    internal struct CommandDispatchSucceededAsynchronously: IDispatchResult
    {
        internal YarnTask Task;

        internal CommandDispatchSucceededAsynchronously(YarnTask task)
        {
            Task = task;
        }
    }
    internal struct FunctionDispatchSucceededAsynchronously<T>: IDispatchResult
    {
        internal YarnTask<T> Task;

        internal FunctionDispatchSucceededAsynchronously(YarnTask<T> task)
        {
            Task = task;
        }
    }
    internal struct FunctionDispatchSucceededSynchronously: IDispatchResult
    {
        internal IConvertible Result;

        internal FunctionDispatchSucceededSynchronously(IConvertible result)
        {
            Result = result;
        }
    }
}
