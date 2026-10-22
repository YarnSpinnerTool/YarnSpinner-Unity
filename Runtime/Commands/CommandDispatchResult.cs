/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// If an action fails in it's dispatch this captures the reason why
    /// </summary>
    internal enum StatusType
    {
        /// <summary>
        /// Was unable to find a valid target for the action.
        /// </summary>
        NoTargetFound,

        /// <summary>
        /// The number of provided parameters does not match the expected number for this action
        /// </summary>
        InvalidParameterCount,

        /// <summary>
        /// The parameter is not of the expected type
        /// </summary>
        InvalidParameter,

        /// <summary>
        /// The action could not be found.
        /// </summary>
        ActionUnknown,

        /// <summary>
        /// The return is not of the expected type
        /// </summary>
        InvalidReturn,
    }

    /// <summary>
    /// Represents any number of different results that could have happened while dispatching an action.
    /// </summary>
    /// <remarks>
    /// This includes all success conditions as well as failure conditions.
    /// </remarks>
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
