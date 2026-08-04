/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Attributes
{
    public class YarnEnumParameterAttribute: YarnParameterAttribute
    {
        public string Name { get; set; }

        public YarnEnumParameterAttribute(string name) => Name = name;
    }

    // this attribute exists in a public spot because different pieces need to access it
    // but it isn't something you should attempt to use
    // to do so may well lead to madness and undefined behaviour
    public sealed class YarnGeneratedEnumAttribute : System.Attribute
    {
        public Yarn.Shared.YarnEnum.BackingType backing;
        public YarnGeneratedEnumAttribute(Yarn.Shared.YarnEnum.BackingType backingType)
        {
            backing = backingType;
        }
    }
}
