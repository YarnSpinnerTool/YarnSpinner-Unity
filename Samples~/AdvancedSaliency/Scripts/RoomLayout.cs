/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using UnityEngine;
using System.Collections.Generic;
using System;

namespace Yarn.Unity.Samples
{
    [System.Serializable]
    public class CharacterSpawn
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [CreateAssetMenu(fileName = "Layout", menuName = "Yarn Spinner/Samples/Advanced Saliency Room Layout")]
    public class RoomLayout : ScriptableObject
    {
        public CharacterSpawn? primary;
        public CharacterSpawn? secondary;
        public GameObject? environmentPrefab;
    }


#if UNITY_EDITOR

    namespace Editor
    {

        using UnityEditor;
        [CustomEditor(typeof(RoomLayout))]
        public class RoomLayoutEditor : UnityEditor.Editor
        {
            GameObject? previewEnvironment;
            protected void OnEnable()
            {

                if (target is not RoomLayout roomLayout)
                {
                    throw new System.InvalidOperationException($"Target is not a {nameof(RoomLayout)}?");
                }

                SceneView.duringSceneGui += DrawSceneGUI;

                if (roomLayout.environmentPrefab != null)
                {
                    var clone = GameObject.Instantiate(roomLayout.environmentPrefab);
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    previewEnvironment = clone;
                }
            }

            protected void OnDisable()
            {
                if (previewEnvironment != null)
                {
                    DestroyImmediate(previewEnvironment);
                    previewEnvironment = null;
                }

                SceneView.duringSceneGui -= DrawSceneGUI;
            }

            private void DrawSceneGUI(SceneView view)
            {
                RoomLayout spawnPoint = (RoomLayout)target;

                Vector3 primaryPoint = Vector3.zero;
                Vector3 secondaryPoint = Vector3.zero;

                Quaternion primaryRotation = Quaternion.identity;
                Quaternion secondaryRotation = Quaternion.identity;

                if (spawnPoint.primary != null)
                {

                    primaryPoint = spawnPoint.primary.position;
                    primaryRotation = spawnPoint.primary.rotation.normalized;

                    primaryPoint = Handles.PositionHandle(primaryPoint, primaryRotation);
                    primaryRotation = Handles.RotationHandle(primaryRotation, primaryPoint);
                }

                if (spawnPoint.secondary != null)
                {

                    secondaryPoint = spawnPoint.secondary.position;
                    secondaryRotation = spawnPoint.secondary.rotation.normalized;

                    secondaryPoint = Handles.PositionHandle(secondaryPoint, secondaryRotation);
                    secondaryRotation = Handles.RotationHandle(secondaryRotation, secondaryPoint);

                }

                Handles.Label(primaryPoint, "Primary");
                Handles.Label(secondaryPoint, "Secondary");

                if (GUI.changed)
                {
                    Undo.RecordObject(target, "Adjusted primary and secondary");

                    if (spawnPoint.primary != null)
                    {

                        spawnPoint.primary.position = primaryPoint;
                        spawnPoint.primary.rotation = primaryRotation;
                    }

                    if (spawnPoint.secondary != null)
                    {
                        spawnPoint.secondary.position = secondaryPoint;
                        spawnPoint.secondary.rotation = secondaryRotation;
                    }
                }
            }
        }
    }
#endif
}