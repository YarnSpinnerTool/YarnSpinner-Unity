using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[System.Serializable]
public class PropSpawn
{
    public string gameObjectName;
    public Vector3 position;
    public Quaternion rotation;
}
[System.Serializable]
public class CharacterSpawn
{
    public Vector3 position;
    public Quaternion rotation;
}

[CreateAssetMenu(fileName = "Layout", menuName = "Yarn Spinner/Samples/Advanced Saliency Room Layout")]
public class RoomLayout : ScriptableObject
{
    public CharacterSpawn primary;
    public CharacterSpawn secondary;
    public PropSpawn[] props;
}


[CustomEditor(typeof(RoomLayout))]
public class RoomLayoutEditor : Editor
{
    Dictionary<string, GameObject> tempObjects = new();
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        foreach (var obj in tempObjects)
        {
            DestroyImmediate(obj.Value);
        }
        tempObjects.Clear();
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView view)
    {
        if (tempObjects == null)
        {
            tempObjects = new();
        }
        RoomLayout spawnPoint = (RoomLayout)target;

        Vector3 primaryPoint = spawnPoint.primary.position;
        Quaternion primaryRotation = spawnPoint.primary.rotation.normalized;

        primaryPoint = Handles.PositionHandle(primaryPoint, primaryRotation);
        primaryRotation = Handles.RotationHandle(primaryRotation, primaryPoint);

        Vector3 secondaryPoint = spawnPoint.secondary.position;
        Quaternion secondaryRotation = spawnPoint.secondary.rotation.normalized;

        secondaryPoint = Handles.PositionHandle(secondaryPoint, secondaryRotation);
        secondaryRotation = Handles.RotationHandle(secondaryRotation, secondaryPoint);

        Handles.Label(primaryPoint, "Primary");
        Handles.Label(secondaryPoint, "Secondary");

        foreach (var prop in spawnPoint.props)
        {
            // find the game object with that name
            // clone it
            // position it
            if (!tempObjects.TryGetValue(prop.gameObjectName, out var clone))
            {
                var template = GameObject.Find(prop.gameObjectName);
                if (template == null)
                {
                    continue;
                }
                clone = GameObject.Instantiate(template, prop.position, prop.rotation);
                clone.hideFlags = HideFlags.HideAndDontSave;
                tempObjects[prop.gameObjectName] = clone;
            }
        }

        if (GUI.changed)
        {
            Undo.RecordObject(target, "Adjusted primary and secondary");

            spawnPoint.primary.position = primaryPoint;
            spawnPoint.primary.rotation = primaryRotation;

            spawnPoint.secondary.position = secondaryPoint;
            spawnPoint.secondary.rotation = secondaryRotation;
        }
    }
}