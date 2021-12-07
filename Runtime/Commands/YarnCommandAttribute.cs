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
using System;

namespace Yarn.Unity
{
    #region Class/Interface

    /// <summary>
    /// An attribute that marks a method on an object as a 
    /// [command](<![CDATA[ {{<ref
    /// "/docs/unity/working-with-commands">}}]]>).
    /// </summary>
    /// <remarks>
    /// When a <see cref="DialogueRunner"/> receives a <see cref="Command"/>,
    /// and no command handler has been installed for the command, it splits it
    /// by spaces, and then checks to see if the second word, if any, is the
    /// name of an object.
    /// 
    /// By default, it checks for any <see cref="GameObject"/>s in the scene.
    /// If one is found, it is checked to see if any of the <see
    /// cref="MonoBehaviour"/>s attached to the class has a <see
    /// cref="YarnCommandAttribute"/> whose <see
    /// cref="YarnCommandAttribute.CommandString"/> matching the first word
    /// of the command.
    /// 
    /// If the method is static, it will not try to inject an object.
    ///
    /// If a method is found, its parameters are checked:
    ///
    /// * If the method takes a single <see cref="string"/>[] parameter, the
    /// method is called, and will be passed an array containing all words in
    /// the command after the first two.
    ///
    /// * If the method takes a number of parameters equal to the number of
    /// words in the command after the first two, it will be called with those
    /// words as parameters.
    /// 
    /// * If a parameter is a <see cref="GameObject"/>, we look up the object
    /// using <see cref="GameObject.Find(string)"/>. As per the API, the game
    /// object must be active.
    /// 
    /// * If a parameter is assignable to <see cref="Component"/>, we will
    /// locate the component based on the name of the object. As per the API of 
    /// <see cref="GameObject.Find(string)"/>, the game object must be active.
    /// If you'd like to have a custom injector for a parameter, use the
    /// <see cref="YarnParameterAttribute"/>.
    /// 
    /// * If a parameter is a <see cref="bool"/>, the string must be 
    /// <c>true</c> or <c>false</c> (as defined by the standard converter for
    /// <see cref="string"/> to <see cref="bool"/>). However, we also allow for
    /// the string to equal the parameter name, case insensitive. This allows
    /// us to write commands with more self-documenting parameters, eg for a
    /// certain <c>Move(bool wait)</c>, you could write 
    /// <c><![CDATA[<<move wait>>]]></c> instead of
    /// <c><![CDATA[<<move true>>]]></c>.
    /// 
    /// * For any other type, we will attempt to convert using
    /// <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> using
    /// the <see cref="System.Globalization.CultureInfo.InvariantCulture"/>
    /// culture. This means that you can implement <see cref="IConvertible"/>
    /// to add new accepted types. (Do be aware that it's a non-CLS compliant
    /// interface, according to its docs. Mono for Unity seems to implement it,
    /// but you may have trouble if you use any other CLS implementation.)
    /// 
    /// * Otherwise, it will not be called, and a warning will be issued.
    ///
    /// ### `YarnCommand`s and Coroutines
    ///
    /// This attribute may be attached to a coroutine. 
    ///
    /// {{|note|}} The <see cref="DialogueRunner"/> determines if the method is
    /// a coroutine if the method returns <see cref="IEnumerator"/>. 
    /// {{|/note|}}
    ///
    /// If the method is a coroutine, the DialogueRunner will pause execution
    /// until the coroutine ends.
    /// </remarks>
    /// <example>
    ///
    /// The following C# code uses the `YarnCommand` attribute to register
    /// commands.
    ///
    /// <![CDATA[```csharp class ExampleBehaviour : MonoBehaviour
    /// {[YarnCommand("jump")] void Jump()
    /// {Debug.Log($"{this.gameObject.name} is jumping!");}
    ///
    ///         [YarnCommand("walk")] void WalkToDestination(string
    ///         destination) {Debug.Log($"{this.gameObject.name} is walking
    ///         to {destination}!");}
    ///
    ///         [YarnCommand("shine_flashlight")] IEnumerator
    ///         ShineFlashlight(string durationString)
    ///         {float.TryParse(durationString, out var duration);
    ///         Debug.Log($"{this.gameObject.name} is turning on the
    ///         flashlight for {duration} seconds!"); yield new
    ///         WaitForSeconds(duration);
    ///         Debug.Log($"{this.gameObject.name} is turning off the
    ///         flashlight!");}} ```]]>
    ///
    /// Next, assume that this `ExampleBehaviour` script has been attached to
    /// a <see cref="GameObject"/> present in the scene named "Mae". The `Jump`
    /// and `WalkToDestination` methods may then be called from a Yarn script
    /// like so:
    ///
    /// <![CDATA[```yarn // Call the Jump() method in the ExampleBehaviour
    /// on Mae <<jump Mae>>
    ///
    /// // Call the WalkToDestination() method in the ExampleBehaviour //
    /// on Mae, passing "targetPoint" as a parameter <<walk Mae
    /// targetPoint>>
    ///
    /// // Call the ShineFlashlight method, passing "0.5" as a parameter;
    /// // dialogue will wait until the coroutine ends. <<shine_flashlight
    /// Mae 0.5>> ```]]>
    ///
    /// Running this Yarn code will result in the following text being logged
    /// to the Console:
    ///
    /// ``` Mae is jumping! Mae is walking to targetPoint! Mae is turning on
    /// the flashlight for 0.5 seconds! (... 0.5 seconds elapse ...) Mae is
    /// turning off the flashlight! ```
    /// </example>
    public class YarnCommandAttribute : YarnActionAttribute
    {
        [Obsolete("Use " + nameof(Name) + " instead.")]
        public string CommandString {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Override the state injector for this command only.
        /// </summary>
        /// <remarks>
        /// If not defined, will use the method marked by
        /// <see cref="YarnStateInjectorAttribute"/> on the class, or if that
        /// is not defined and the class subclasses
        /// <see cref="MonoBehaviour"/>, using
        /// <see cref="UnityEngine.GameObject.Find(string)"/>.
        /// 
        /// If none of those conditions are true, but the function is not
        /// static, an error will be thrown. However, if the function is
        /// indeed static, this parameter will be ignored.
        /// </remarks>
        public string Injector { get; set; }

        public YarnCommandAttribute(string name = null) => Name = name;
    }
    #endregion
}
