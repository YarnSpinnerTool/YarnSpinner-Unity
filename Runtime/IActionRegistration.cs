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

using UnityEngine;
using System.Collections;
using System.Reflection;
using System;

namespace Yarn.Unity
{
    public interface IActionRegistration {
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
        void AddCommandHandler(string commandName, Delegate handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        /// <param name="methodInfo">The method that will be invoked when the
        /// command is called.</param>
        void AddCommandHandler(string commandName, MethodInfo methodInfo);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler(string commandName, System.Func<Coroutine> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1>(string commandName, System.Func<T1, Coroutine> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, Coroutine> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, Coroutine> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, Coroutine> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, Coroutine> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, Coroutine> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler(string commandName, System.Func<IEnumerator> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1>(string commandName, System.Func<T1, IEnumerator> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, IEnumerator> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, IEnumerator> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, IEnumerator> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, IEnumerator> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler(string commandName, System.Action handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1>(string commandName, System.Action<T1> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler);


        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler);

        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to
        /// remove.</param>
        void RemoveCommandHandler(string commandName);

        /// <summary>
        /// Add a new function that returns a value, so that it can be
        /// called from Yarn scripts.
        /// </summary>
        /// <remarks>
        /// <para>When this function has been registered, it can be called from
        /// your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
        ///     myFunction returned true!
        /// &lt;&lt;endif&gt;&gt;
        /// </code>
        ///
        /// <para>The <c>call</c> command can also be used to invoke the function:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;call myFunction(1, 2)&gt;&gt;
        /// </code>
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        void AddFunction(string name, Delegate implementation);


        /// <inheritdoc cref="AddFunction(string, Delegate)" />
        /// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
        void AddFunction<TResult>(string name, System.Func<TResult> implementation);

        /// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
        /// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
        void AddFunction<TResult, T1>(string name, System.Func<TResult, T1> implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1}(string, Func{TResult,T1})" />
        /// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
        void AddFunction<TResult, T1, T2>(string name, System.Func<TResult, T1, T2> implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2}(string, Func{TResult,T1,T2})" />
        /// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
        void AddFunction<TResult, T1, T2, T3>(string name, System.Func<TResult, T1, T2, T3> implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3}(string, Func{TResult,T1,T2,T3})" />
        /// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
        void AddFunction<TResult, T1, T2, T3, T4>(string name, System.Func<TResult, T1, T2, T3, T4> implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4}(string, Func{TResult,T1,T2,T3,T4})" />
        /// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
        void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, System.Func<TResult, T1, T2, T3, T4, T5> implementation);

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4,T5}(string, Func{TResult,T1,T2,T3,T4,T5})" />
        /// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
        void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, System.Func<TResult, T1, T2, T3, T4, T5, T6> implementation);

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from
        /// Yarn scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
        void RemoveFunction(string name);
    }
}
