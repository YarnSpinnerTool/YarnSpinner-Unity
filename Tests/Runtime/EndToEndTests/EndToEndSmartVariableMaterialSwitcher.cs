/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Yarn.Unity.Tests
{

    public class EndToEndSmartVariableMaterialSwitcher : MonoBehaviour
    {
        [SerializeField] VariableStorageBehaviour variableStorage;
        [SerializeField] string smartVariableName;

        [SerializeField] Material variableFalse;
        [SerializeField] Material variableTrue;

        void Update()
        {
            if (variableStorage.TryGetValue<bool>(smartVariableName, out var result) == false)
            {
                Debug.LogWarning($"Failed to get a value for {smartVariableName}!");
                this.gameObject.SetActive(false);
                return;
            }

            if (TryGetComponent<Renderer>(out var renderer) == false)
            {
                return;
            }

            switch (result)
            {
                case true:
                    renderer.material = variableTrue;
                    break;
                case false:
                    renderer.material = variableFalse;
                    break;
            }
        }
    }
}
