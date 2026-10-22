/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Attributes
{
    /// <summary>
    /// Flags a string parameter in a command or function as one that the editor should offer autocomplete support for enums.
    /// This is just a convenience mechanism, it doesn't change any behaviour of the action in any way.
    /// </summary>
    public class YarnEnumParameterAttribute: YarnParameterAttribute
    {
        public string Name { get; set; }

        public YarnEnumParameterAttribute(string name) => Name = name;
    }

    /// <summary>
    /// Flags an enum as having been exported from the <see cref="VariableStorageBehaviour"/> generated script.
    /// </summary>
    /// <remarks>
    /// This attribute exists in a public spot because different pieces of Yarn Spinner need to access it, but it isn't something you should attempt to use.
    /// To do so may well lead to madness and undefined behaviour
    /// </remarks>
    public sealed class YarnGeneratedEnumAttribute : System.Attribute
    {
        public Yarn.Shared.YarnEnum.BackingType backing;
        public YarnGeneratedEnumAttribute(Yarn.Shared.YarnEnum.BackingType backingType)
        {
            backing = backingType;
        }
    }
}
