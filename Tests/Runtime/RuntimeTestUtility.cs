using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yarn.Unity.Tests
{
    public static class RuntimeTestUtility
    {
        public static void AddSceneToBuild(string GUID)
        {
#if UNITY_EDITOR
            // Is a scene with this GUID already in the list?
            if (EditorBuildSettings.scenes.Any(x => x.guid.ToString() == GUID))
            {
                // Then there's nothing to do!
                return;
            }

            // Add the test scene 
            var dialogueRunnerTestScene = new EditorBuildSettingsScene(new GUID(GUID), true);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { dialogueRunnerTestScene }).ToArray();
#endif
        }

        public static void RemoveSceneFromBuild(string GUID)
        {
#if UNITY_EDITOR
            // Filter the list to remove any scene with this GUID
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(x => x.guid.ToString() != GUID).ToArray();
#endif
        }
    }
}
