using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{

    public partial class DialogueRunner : IActionRegistration
    {
        /// <summary>
        /// Adds a command handler. Dialogue will pause execution after the
        /// command is called.
        /// </summary>
        /// <remarks>
        /// <para>When this command handler has been added, it can be called
        /// from your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;commandName param1 param2&gt;&gt;
        /// </code>
        ///
        /// <para>If <paramref name="handler"/> is a method that returns a <see
        /// cref="Coroutine"/>, when the command is run, the <see
        /// cref="DialogueRunner"/> will wait for the returned coroutine to stop
        /// before delivering any more content.</para>
        /// <para>If <paramref name="handler"/> is a method that returns an <see
        /// cref="IEnumerator"/>, when the command is run, the <see
        /// cref="DialogueRunner"/> will start a coroutine using that method and
        /// wait for that coroutine to stop before delivering any more content.
        /// </para>
        /// </remarks>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The <see cref="CommandHandler"/> that will be
        /// invoked when the command is called.</param>
        public void AddCommandHandler(string commandName, Delegate handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        /// <param name="method">The method that will be invoked when the
        /// command is called.</param>
        public void AddCommandHandler(string commandName, MethodInfo method) => CommandDispatcher.AddCommandHandler(commandName, method);
        
        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to remove.</param>
        public void RemoveCommandHandler(string commandName) => CommandDispatcher.RemoveCommandHandler(commandName);


        /// <summary>
        /// Add a new function that returns a value, so that it can be called
        /// from Yarn scripts.
        /// </summary>
        /// <remarks>
        /// <para>When this function has been registered, it can be called from
        /// your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;if myFunction(1, 2) == true&gt;&gt; myFunction returned
        /// true! &lt;&lt;endif&gt;&gt;
        /// </code>
        ///
        /// <para>The <c>call</c> command can also be used to invoke the
        /// function:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;call myFunction(1, 2)&gt;&gt;
        /// </code>
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that should
        /// be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        public void AddFunction(string name, Delegate implementation) => CommandDispatcher.AddFunction(name, implementation);

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from Yarn
        /// scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
        public void RemoveFunction(string name) => CommandDispatcher.RemoveFunction(name);
    }
}
