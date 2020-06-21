using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yarn.Unity.Example {
    /// <summary>Simple utility script for Yarn Spinner 3D example, triggers a UnityEvent upon key press.
    /// This is useful for, like, pressing SPACE to continue the dialogue.</summary>
    public class KeyboardEventDispatch : MonoBehaviour
    {
        public KeyCode keyToPress = KeyCode.Space; // set this in the Inspector
        public UnityEvent onKeyPressed;

        // Update is called once per frame
        void Update()
        {
            if ( Input.GetKeyDown(keyToPress) ) {
                onKeyPressed.Invoke();
            }
        }
    }
}
