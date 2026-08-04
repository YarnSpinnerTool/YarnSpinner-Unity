/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using System;

namespace Yarn.Unity
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class YarnConverterAttribute : Attribute
    {
        public Type ConversionSupport { get; set; }

        public YarnConverterAttribute(Type type) => ConversionSupport = type;
    }
}
