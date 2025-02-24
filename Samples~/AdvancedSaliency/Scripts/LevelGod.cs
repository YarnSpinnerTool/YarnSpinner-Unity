/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

namespace Yarn.Unity.Samples
{
    public class LevelGod : MonoBehaviour
    {
        [MustNotBeNull]
        public TheRoomVariableStorage variableStorage;
        public DialogueRunner? runner;
        public SpawnPointData[] spawns = Array.Empty<SpawnPointData>();

        private GameObject? currentEnvironment;
        public void SpawnLevel()
        {
            if (currentEnvironment != null)
            {
                Destroy(currentEnvironment);
                currentEnvironment = null;
            }

            RoomLayout? config = null;

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

            if (config.primary != null)
            {
                primary.transform.SetPositionAndRotation(config.primary.position, config.primary.rotation);

                if (primary.TryGetComponent<SimpleCharacter>(out var character))
                {
                    character.SetLookDirection(config.primary.rotation, immediate: true);
                }
            }

            if (config.secondary != null)
            {
                secondary.transform.SetPositionAndRotation(config.secondary.position, config.secondary.rotation);

                if (secondary.TryGetComponent<SimpleCharacter>(out var character))
                {
                    character.SetLookDirection(config.secondary.rotation, immediate: true);
                }
            }

            if (config.environmentPrefab != null)
            {
                currentEnvironment = Instantiate(config.environmentPrefab);
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
