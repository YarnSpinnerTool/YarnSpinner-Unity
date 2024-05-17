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

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Func<object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

        // GYB14 START / <inheritdoc cref="AddCommandHandler(string,
        //Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Func<T1, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, object> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        // GYB14 END

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Action handler) => CommandDispatcher.AddCommandHandler(commandName, handler);

        // GYB15 START / <inheritdoc cref="AddCommandHandler(string,
        //Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Action<T1> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler) => CommandDispatcher.AddCommandHandler(commandName, handler);
        // GYB15 END

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


        /// <inheritdoc cref="AddFunction(string, Delegate)" />
        /// <typeparam name="TResult">The type of the value that the function
        /// should return.</typeparam>
        public void AddFunction<TResult>(string name, System.Func<TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);

        // GYB16 START / <inheritdoc cref="AddFunction{TResult}(string,
        //Func{TResult})" /> / <typeparam name="T1">The type of the first
        //parameter to the function.</typeparam>
        public void AddFunction<T1, TResult>(string name, System.Func<T1, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,TResult}(string, Func{T1,TResult})"
        /// />
        /// <typeparam name="T2">The type of the second parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, TResult>(string name, System.Func<T1, T2, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,TResult}(string,
        /// Func{T1,T2,TResult})" />
        /// <typeparam name="T3">The type of the third parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, TResult>(string name, System.Func<T1, T2, T3, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,T3,TResult}(string,
        /// Func{T1,T2,T3,TResult})" />
        /// <typeparam name="T4">The type of the fourth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, TResult>(string name, System.Func<T1, T2, T3, T4, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,T3,T4,TResult}(string,
        /// Func{T1,T2,T3,T4,TResult})" />
        /// <typeparam name="T5">The type of the fifth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, System.Func<T1, T2, T3, T4, T5, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,TResult}(string,
        /// Func{T1,T2,T3,T4,T5,TResult})" />
        /// <typeparam name="T6">The type of the sixth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,T6,TResult}(string,
        /// Func{T1,T2,T3,T4,T5,T6,TResult})" />
        /// <typeparam name="T7">The type of the seventh parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc cref="AddFunction{T1,T2,T3,T4,T5,T6,T7,TResult}(string,
        /// Func{T1,T2,T3,T4,T5,T6,T7,TResult})" />
        /// <typeparam name="T8">The type of the eighth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc
        /// cref="AddFunction{T1,T2,T3,T4,T5,T6,T7,T8,TResult}(string,
        /// Func{T1,T2,T3,T4,T5,T6,T7,T8,TResult})" />
        /// <typeparam name="T9">The type of the ninth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        /// <inheritdoc
        /// cref="AddFunction{T1,T2,T3,T4,T5,T6,T7,T8,T9,TResult}(string,
        /// Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,TResult})" />
        /// <typeparam name="T10">The type of the tenth parameter to the
        /// function.</typeparam>
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> implementation) => CommandDispatcher.AddFunction(name, implementation);
        // GYB16 END


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
