/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

#nullable enable

namespace Yarn.Unity.Tests {
    public class EndToEndTestNPC : MonoBehaviour {
        public string? nodeName = null;

        public void Awake() {
            var label = this.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) {
                label.text = this.nodeName;
            }
        }
    }
}
