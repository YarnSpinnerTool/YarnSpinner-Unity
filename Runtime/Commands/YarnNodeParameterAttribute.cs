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
    /// Flags a string parameter in a command or function as one that the editor should offer autocomplete support for node names.
    /// This is just a convenience mechanism, it doesn't change any behaviour of the action in any way.
    /// </summary>
    public class YarnNodeParameterAttribute: YarnParameterAttribute {}
}
