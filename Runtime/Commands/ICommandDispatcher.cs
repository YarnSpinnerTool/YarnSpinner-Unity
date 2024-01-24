/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    interface ICommandDispatcher : IActionRegistration
    {
        CommandDispatchResult DispatchCommand(string command, out Coroutine commandCoroutine);

        void SetupForProject(YarnProject yarnProject);

        IEnumerable<ICommand> Commands { get; }
    }
}
