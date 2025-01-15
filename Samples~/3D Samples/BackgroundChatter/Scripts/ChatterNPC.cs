#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class ChatterNPC : MonoBehaviour
    {
        public string? NPCName;

        [SerializeField] Transform? backgroundChatterPoint;

        public Transform BackgroundChatterPoint => backgroundChatterPoint != null ? backgroundChatterPoint : this.transform;

        public static ChatterNPC? FindByName(string name)
        {
            var allNPCs = FindObjectsByType<ChatterNPC>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var npc in allNPCs)
            {
                if (npc.NPCName == name)
                {
                    return npc;
                }
            }
            return null;
        }
    }

}