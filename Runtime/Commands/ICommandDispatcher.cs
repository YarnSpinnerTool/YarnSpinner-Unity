using System;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    interface ICommandDispatcher : IActionRegistration
    {
        public DialogueRunner.CommandDispatchResult DispatchCommand(string command, out Coroutine? commandCoroutine);

        public void SetupForProject(YarnProject yarnProject);
    }
}
