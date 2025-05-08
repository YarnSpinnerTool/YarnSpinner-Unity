/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    #region Class/Interface

    /// <summary>
    /// An attribute that marks a method on an object as a command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <see cref="DialogueRunner"/> receives a <see cref="Command"/>,
    /// and no command handler has been installed for the command, it splits it
    /// by spaces, and then checks to see if the second word, if any, is the
    /// name of an object.
    /// </para>
    /// <para>
    /// By default, it checks for any <see cref="GameObject"/>s in the scene. If
    /// one is found, it is checked to see if any of the <see
    /// cref="MonoBehaviour"/>s attached to the class has a <see
    /// cref="YarnCommandAttribute"/> whose <see
    /// cref="YarnActionAttribute.Name"/> matches the first word of the command.
    /// </para>
    /// <para>If the method is static, it will not try to use an object.</para>
    /// <para>If a method is found, its parameters are checked:</para>
    /// <list type="bullet">
    /// <item>
    /// If the method takes a single <see cref="string"/>[] parameter, the
    /// method is called, and will be passed an array containing all words in
    /// the command after the first two.
    /// </item>
    /// <item>
    /// If the method takes a number of parameters equal to the number of words
    /// in the command after the first two, it will be called with those words
    /// as parameters.
    /// </item>
    /// <item>
    /// If a parameter is a <see cref="GameObject"/>, we look up the object
    /// using <see cref="GameObject.Find(string)"/>. As per the API, the game
    /// object must be active.
    /// </item>
    /// <item>
    /// If a parameter is assignable to <see cref="Component"/>, we will locate
    /// the component based on the name of the object. As per the API of <see
    /// cref="GameObject.Find(string)"/>, the game object must be active.
    /// </item>
    /// <item>
    /// If a parameter is a <see cref="bool"/>, the string must be <c>true</c>
    /// or <c>false</c> (as defined by the standard converter for <see
    /// cref="string"/> to <see cref="bool"/>). However, we also allow for the
    /// string to equal the parameter name, case insensitive. This allows us to
    /// write commands with more self-documenting parameters, eg for a certain
    /// <c>Move(bool wait)</c>, you could write <c>&lt;&lt;move wait&gt;&gt;</c>
    /// instead of <c>&lt;&lt;move true&gt;&gt;</c>.
    /// </item>
    /// <item>
    /// For any other type, we will attempt to convert using <see
    /// cref="Convert.ChangeType(object, Type, IFormatProvider)"/> using the
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture"/> culture.
    /// This means that you can implement <see cref="IConvertible"/> to add new
    /// accepted types. (Do be aware that it's a non-CLS compliant interface,
    /// according to its docs. Mono for Unity seems to implement it, but you may
    /// have trouble if you use any other CLS implementation.)
    /// </item>
    /// <item>Otherwise, it will not be called, and a warning will be
    /// issued.</item>
    /// </list>
    /// <para>This attribute may be attached to a coroutine or to an async
    /// method.</para>
    /// <para style="note">
    /// The <see cref="DialogueRunner"/> determines if the method is a coroutine
    /// if the method returns <see cref="IEnumerator"/>, or if the method
    /// returns a <see cref="Coroutine"/> or a task.
    /// </para>
    /// <para>
    /// If the method is a coroutine, returns a <see cref="Coroutine"/>, or
    /// returns a task, the DialogueRunner will pause execution until the the
    /// coroutine or task ends.
    /// </para>
    /// </remarks>
    public class YarnCommandAttribute : YarnActionAttribute
    {
        /// <inheritdoc/>
        public YarnCommandAttribute(string? name = null) => Name = name;
    }
    #endregion
}
