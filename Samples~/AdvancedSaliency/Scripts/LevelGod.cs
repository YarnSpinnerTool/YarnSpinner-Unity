using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Samples
{
    public class LevelGod : MonoBehaviour
    {
        public TheRoomVariableStorage variableStorage;
        public DialogueRunner runner;
        public SpawnPointData[] spawns;
        public void SpawnLevel()
        {
            // we always start every level by making sure there is nothing in the room itself
            // so we do a bulk return for everything
            var returners = GameObject.FindObjectsByType<VoidReturner>(FindObjectsSortMode.None);
            foreach (var returner in returners)
            {
                Debug.Log($"returning {returner.name}");
                returner.ReturnToTheVoid();
            }

            RoomLayout config = null;
            foreach (var spawn in spawns)
            {
                if (spawn.name == variableStorage.Room)
                {
                    config = spawn.data;
                    break;
                }
            }

            if (config == null)
            {
                Debug.LogError($"Unable to load a layout configuration for {variableStorage.Room}");
                return;
            }

            var primary = GameObject.Find(variableStorage.Primary.GetBackingValue());
            var secondary = GameObject.Find(variableStorage.Secondary.GetBackingValue());
            if (primary == null || secondary == null)
            {
                Debug.Log("failed to find one (or both) of the characters");
                return;
            }

            primary.transform.position = config.primary.position;
            primary.transform.rotation = config.primary.rotation;
            secondary.transform.position = config.secondary.position;
            secondary.transform.rotation = config.secondary.rotation;

            foreach (var prop in config.props)
            {
                var model = GameObject.Find(prop.gameObjectName);
                if (model == null)
                {
                    Debug.Log($"failed to find the {prop.gameObjectName} prop");
                    continue;
                }
                model.transform.position = prop.position;
                model.transform.rotation = prop.rotation;
            }
        }

        void Start()
        {
            runner.AddCommandHandler("start_level", SpawnLevel);
        }
    }

    [System.Serializable]
    public struct SpawnPointData
    {
        public Room name;
        public RoomLayout data;
    }
}
