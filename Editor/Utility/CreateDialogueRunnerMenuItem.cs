/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using Yarn.Unity;

    /// <summary>
    /// Adds a menu item to the menu bar for creating an instance of the
    /// dialogue runner prefab.
    /// </summary>
    public static class CreateDialogueRunnerMenuItem
    {
        const string DialogueRunnerPrefabGUID = "52571f68872914e24837210513edea1d";

        /// <summary>
        /// Instantiates the Dialogue System prefab in the currently active scene,
        /// and returns the created <see cref="DialogueRunner"/>.
        /// </summary>
        /// <returns>A newly created <see cref="DialogueRunner"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the
        /// Dialogue System prefab cannot be found in the Yarn Spinner
        /// package.</exception>
        [MenuItem("GameObject/Yarn Spinner/Dialogue System", priority = 11)]
        public static DialogueRunner CreateDialogueRunner()
        {

            string assetPath = AssetDatabase.GUIDToAssetPath(DialogueRunnerPrefabGUID);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefabAsset == null)
            {
                throw new System.InvalidOperationException(
                    $"Can't create a new Dialogue System: Can't find the prefab to create a Dialogue System from."
                );
            }

            var instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

#if UNITY_2023_1_OR_NEWER
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
#else
            var eventSystems = Object.FindObjectsOfType<EventSystem>();
#endif

            if (eventSystems.Length > 1)
            {
                // At least one other event system is present in the scene. Turn off
                // the one that came with the prefab - it's not needed.
                var instantiatedEventSystem = instantiatedPrefab.GetComponentInChildren<EventSystem>();

                instantiatedEventSystem.gameObject.SetActive(false);
            }

            return instantiatedPrefab.GetComponent<DialogueRunner>();
        }
    }
}
