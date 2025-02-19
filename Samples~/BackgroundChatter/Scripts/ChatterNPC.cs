/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class ChatterNPC : MonoBehaviour
    {
        [SerializeField] Transform? backgroundChatterPoint;

        public Transform BackgroundChatterPoint => backgroundChatterPoint != null ? backgroundChatterPoint : this.transform;

        public static ChatterNPC? FindByName(string name)
        {
            var allNPCs = FindObjectsByType<ChatterNPC>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var npc in allNPCs)
            {
                if (npc.name == name)
                {
                    return npc;
                }
            }
            return null;
        }
    }

}