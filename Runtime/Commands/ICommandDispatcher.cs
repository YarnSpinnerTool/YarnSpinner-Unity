using System;
using UnityEngine;

namespace Yarn.Unity
{
    interface ICommandDispatcher : IActionRegistration
    {
        DialogueRunner.CommandDispatchResult DispatchCommand(string command, out Coroutine commandCoroutine);

        void SetupForProject(YarnProject yarnProject);
    }
}
