/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using System.Collections.Generic;

#if USE_TMP
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity
{
    public static class InputSystemAvailability
    {
#if USE_INPUTSYSTEM
        internal const bool inputSystemInstalled = true;
#else
        internal const bool inputSystemInstalled = false;
#endif

#if ENABLE_INPUT_SYSTEM
        internal const bool enableInputSystem = true;
#else
        internal const bool enableInputSystem = false;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        internal const bool enableLegacyInput = true;
#else
        internal const bool enableLegacyInput = false;
#endif

#if !ENABLE_LEGACY_INPUT_MANAGER
        /// <summary>
        /// A dictionary mapping legacy keycodes to Input System keys.
        /// </summary>
        static System.Lazy<Dictionary<KeyCode, UnityEngine.InputSystem.Key>> lookup = new(() =>
        {
            var result = new Dictionary<KeyCode, UnityEngine.InputSystem.Key>();
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                // Attempt to automatically find the equivalent of keyCode by
                // assuming that the string representation of a key (e.g. "Tab")
                // is the same in both enums.
                if (System.Enum.TryParse<UnityEngine.InputSystem.Key>(keyCode.ToString(), true, out var value))
                {
                    result[keyCode] = value;
                }
            }
            // Manually map some remaining keys
            result[KeyCode.Return] = UnityEngine.InputSystem.Key.Enter;
            result[KeyCode.KeypadEnter] = UnityEngine.InputSystem.Key.NumpadEnter;
            return result;
        });
#endif

        /// <summary>
        /// Gets a value indicating whether the key indicated by a <see
        /// cref="KeyCode"/> was pressed this frame.
        /// </summary>
        /// <remarks>
        /// If the Legacy Input Manager is enabled, this method wraps <see
        /// cref="Input.GetKeyDown"/>. Otherwise, it attempts to find the <see
        /// cref="UnityEngine.InputSystem.Key"/> equivalent of <paramref
        /// name="key"/>, and then checks with <see
        /// cref="UnityEngine.InputSystem.Keyboard.current"/> to find the key,
        /// and queries its <see
        /// cref="UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame"/>
        /// property.
        /// </remarks>
        /// <param name="key">The <see cref="KeyCode"/> to check for the state
        /// of.</param>
        /// <returns>Whether the key was pressed this frame.</returns>
        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                // The 'none' key is never pressed
                return false;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            // If we're using Legacy Input, read from it directly
            return Input.GetKeyDown(key);
#else

            if (lookup.Value.TryGetValue(key, out var lookupKey))
            {
                try
                {
                    return UnityEngine.InputSystem.Keyboard.current[lookup.Value[key]].wasPressedThisFrame;

                }
                catch (System.ArgumentOutOfRangeException)
                {
#if DEBUG
                    Debug.LogWarning($"Can't check if {key} is down: found Input System mapping {lookupKey}, but this key is not present in the current keyboard");
#endif
                    return false;
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning($"Can't check if {key} is down: can't find a mapping from legacy keycode {key} to Unity Input System");
#endif
                return false;
            }
#endif
        }

        public static bool GetButtonDown(string? buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return false;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonUp(buttonName);
#else
            return false;
#endif
        }

        public static float GetAxis(string? axisName)
        {
            if (string.IsNullOrEmpty(axisName))
            {
                return 0;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis(axisName);
#else
            return 0;
#endif
        }
    }
}
