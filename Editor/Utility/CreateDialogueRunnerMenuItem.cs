namespace Yarn.Unity.Editor
{
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.EventSystems;
    using Yarn.Unity;

    public static class CreateDialogueRunnerMenuItem
    {
        const string DialogueRunnerPrefabGUID = "7f29e5f7ffdea4a6793cefb278b61f0c";

        /// <summary>
        /// Instantiates the Dialogue System prefab in the currently active scene,
        /// and returns the created <see cref="DialogueRunner"/>.
        /// </summary>
        /// <returns>A newly created <see cref="DialogueRunner"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the
        /// Dialogue System prefab cannot be found in the Yarn Spinner
        /// package.</exception>
        [MenuItem("GameObject/Yarn Spinner/Dialogue Runner", priority = 1)]
        public static DialogueRunner CreateDialogueRunner()
        {

            string assetPath = AssetDatabase.GUIDToAssetPath(DialogueRunnerPrefabGUID);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefabAsset == null)
            {
                throw new System.InvalidOperationException(
                    $"Can't create a new Dialogue Runner: Can't find the prefab to create a Dialogue Runner from."
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
