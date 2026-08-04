/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Reflection;

#nullable enable

namespace Yarn.Unity
{
    public partial class DialogueRunner : IActionRegistration
    {
        /// <inheritdoc />
        public void AddCommandHandler(string commandName, Delegate handler) => ActionInvoker.AddCommandHandler(commandName, handler);

        /// <inheritdoc />
        public void AddCommandHandler(string commandName, MethodInfo method) => ActionInvoker.AddCommandHandler(commandName, method);

        /// <inheritdoc />
        public void RemoveCommandHandler(string commandName) => ActionInvoker.RemoveCommandHandler(commandName);

        /// <inheritdoc />
        public void AddFunction(string name, Delegate implementation) => ActionInvoker.AddFunction(name, implementation);

        /// <inheritdoc />
        public void RemoveFunction(string name) => ActionInvoker.RemoveFunction(name);
    }
}
