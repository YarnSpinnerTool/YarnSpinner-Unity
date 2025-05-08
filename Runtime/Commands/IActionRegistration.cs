/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Contains methods that allow adding and removing Yarn commands and
    /// functions.
    /// </summary>
    public interface IActionRegistration
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
        void AddCommandHandler(string commandName, Delegate handler);

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="methodInfo">The method that will be invoked when the
        /// command is called.</param>
        void AddCommandHandler(string commandName, MethodInfo methodInfo);

        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to remove.</param>
        void RemoveCommandHandler(string commandName);

        /// <summary>
        /// Add a new function that returns a value, so that it can be called
        /// from Yarn scripts.
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
        /// <param name="name">The name of the function to add.</param>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        void AddFunction(string name, Delegate implementation);

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from
        /// Yarn scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction(string, Delegate)"/>
        void RemoveFunction(string name);

        /// <summary>
        /// Registers a function as existing, without supplying an implementation.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="returnType">The return type of the function.</param>
        /// <param name="parameterTypes">The types of the function's parameters.</param>
        void RegisterFunctionDeclaration(string name, Type returnType, Type[] parameterTypes);
    }

    /// <summary>
    /// Contains extension methods for <see cref="IActionRegistration"/>
    /// objects.
    /// </summary>
    public static class ActionRegistrationExtension
    {


        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler(this IActionRegistration registration, string commandName, System.Action handler) => registration.AddCommandHandler(commandName, (Delegate)handler);

        // GYB11 START
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1>(this IActionRegistration registration, string commandName, System.Action<T1> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2>(this IActionRegistration registration, string commandName, System.Action<T1, T2> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IActionRegistration registration, string commandName, System.Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        // GYB11 END

        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1>(this IActionRegistration registration, string commandName, System.Func<T1, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2>(this IActionRegistration registration, string commandName, System.Func<T1, T2, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, System.Threading.Tasks.Task> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);

        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler(this IActionRegistration registration, string commandName, System.Func<YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1>(this IActionRegistration registration, string commandName, System.Func<T1, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2>(this IActionRegistration registration, string commandName, System.Func<T1, T2, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, YarnTask> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        // GYB11 END

        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler(this IActionRegistration registration, string commandName, System.Func<IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1>(this IActionRegistration registration, string commandName, System.Func<T1, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2>(this IActionRegistration registration, string commandName, System.Func<T1, T2, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IEnumerator> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        // GYB11 END


        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler(this IActionRegistration registration, string commandName, System.Func<Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1>(this IActionRegistration registration, string commandName, System.Func<T1, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2>(this IActionRegistration registration, string commandName, System.Func<T1, T2, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        /// <inheritdoc cref="IActionRegistration.AddCommandHandler(string, Delegate)"/>
        public static void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IActionRegistration registration, string commandName, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Coroutine> handler) => registration.AddCommandHandler(commandName, (Delegate)handler);
        // GYB11 END


        /// <inheritdoc cref="IActionRegistration.AddFunction(string, Delegate)"/>
        /// <typeparam name="TResult">The result of the function.</typeparam>
        public static void AddFunction<TResult>(this IActionRegistration registration, string name, System.Func<TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, TResult>(this IActionRegistration registration, string name, System.Func<T1, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

        /// <inheritdoc cref="AddFunction{TResult}(IActionRegistration, string, Func{TResult})"/>
        public static void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(this IActionRegistration registration, string name, System.Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> implementation) => registration.AddFunction(name, (Delegate)implementation);

    }
}
