/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Attributes
{
    /// <summary>
    /// abstract parent class for all attributes on yarn action parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public abstract class YarnParameterAttribute : Attribute { }
}
