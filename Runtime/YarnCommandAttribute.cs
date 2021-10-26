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
    /// An attribute that marks a method on a <see cref="MonoBehaviour"/>
    /// as a [command](<![CDATA[ {{<ref
    /// "/docs/unity/working-with-commands">}}]]>).
    /// </summary>
    /// <remarks>
    /// When a <see cref="DialogueRunner"/> receives a <see
    /// cref="Command"/>, and no command handler has been installed for the
    /// command, it splits it by spaces, and then checks to see if the
    /// second word, if any, is the name of any <see cref="GameObject"/>s
    /// in the scene. 
    ///
    /// If one is found, it is checked to see if any of the <see
    /// cref="MonoBehaviour"/>s attached to the class has a <see
    /// cref="YarnCommandAttribute"/> whose <see
    /// cref="YarnCommandAttribute.CommandString"/> matching the first word
    /// of the command.
    ///
    /// If a method is found, its parameters are checked:
    ///
    /// * If the method takes a single <see cref="string"/>[] parameter,
    /// the method is called, and will be passed an array containing all
    /// words in the command after the first two.
    ///
    /// * If the method takes a number of <see cref="string"/> parameters
    /// equal to the number of words in the command after the first two, it
    /// will be called with those words as parameters.
    ///
    /// * Otherwise, it will not be called, and a warning will be issued.
    ///
    /// ### `YarnCommand`s and Coroutines
    ///
    /// This attribute may be attached to a coroutine. 
    ///
    /// {{|note|}} The <see cref="DialogueRunner"/> determines if the
    /// method is a coroutine if the method returns <see
    /// cref="IEnumerator"/>. {{|/note|}}
    ///
    /// If the method is a coroutine, the DialogueRunner will pause
    /// execution until the coroutine ends.
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
    /// Next, assume that this `ExampleBehaviour` script has been attached
    /// to a <see cref="GameObject"/> present in the scene named "Mae". The
    /// `Jump` and `WalkToDestination` methods may then be called from a
    /// Yarn script like so:
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
    /// Running this Yarn code will result in the following text being
    /// logged to the Console:
    ///
    /// ``` Mae is jumping! Mae is walking to targetPoint! Mae is turning
    /// on the flashlight for 0.5 seconds! (... 0.5 seconds elapse ...) Mae
    /// is turning off the flashlight! ```
    /// </example>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class YarnCommandAttribute : System.Attribute
    {
        /// <summary>
        /// The name of the command, as it exists in Yarn.
        /// </summary>
        /// <remarks>
        /// This value does not have to be the same as the name of the
        /// method. For example, you could have a method named
        /// "`WalkToPoint`", and expose it to Yarn as a command named
        /// "`walk_to_point`".
        /// </remarks>        
        public string CommandString { get; set; }

        public YarnCommandAttribute(string commandString) => CommandString = commandString;
    }

#endregion
}
