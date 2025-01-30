using UnityEngine;

namespace Yarn.Unity.Samples.Editor
{
    using System.Buffers;
    using System.Linq;
    using UnityEditor;

    [CustomEditor(typeof(SimplePathMovement))]
    [CanEditMultipleObjects]
    public class SimplePathMovementEditor : Yarn.Unity.Editor.YarnEditor
    {
        public void OnSceneGUI()
        {
            var path = target as SimplePathMovement;
            if (path == null)
            {
                return;
            }

            SimplePathEditor.DrawPath(path.path, showAddButtons: false);
        }
    }

    [CustomEditor(typeof(SimplePath))]
    [CanEditMultipleObjects]
    public class SimplePathEditor : Yarn.Unity.Editor.YarnEditor
    {

        public void OnSceneGUI()
        {
            var path = target as SimplePath;
            if (path == null)
            {
                return;
            }

            DrawPath(path, showAddButtons: true);

            for (int i = 0; i < path.pathElements.Count; i++)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var newPos = Handles.PositionHandle(path.transform.TransformPoint(path.pathElements[i].position), Quaternion.identity);
                    if (change.changed)
                    {
                        newPos = path.transform.InverseTransformPoint(newPos);

                        Undo.RecordObject(path, "move path point");

                        var p = path.pathElements[i];
                        p.position = newPos;
                        path.pathElements[i] = p;
                    }
                }
            }

        }

        internal static void DrawPath(SimplePath path, bool showAddButtons)
        {
            if (path.pathElements.Count >= 2)
            {
                var points = path.pathElements.Append(path.pathElements.First()).Select(p => path.transform.TransformPoint(p.position)).ToArray();
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Handles.DrawLine(
                        points[i], points[i + 1], 2
                    );

                    if (showAddButtons)
                    {
                        Handles.BeginGUI();
                        var buttonSize = new Vector2(25, 25);
                        var midpoint = (points[i] + points[i + 1]) * 0.5f;
                        var buttonPosition = HandleUtility.WorldToGUIPoint(midpoint);
                        var buttonRect = new Rect(buttonPosition - buttonSize * 0.5f, buttonSize);
                        if (GUI.Button(buttonRect, "+"))
                        {
                            Undo.RecordObject(path, "insert path position");
                            path.pathElements.Insert(i + 1, new SimplePath.Position
                            {
                                position = path.transform.InverseTransformPoint(midpoint)
                            });
                        }
                        Handles.EndGUI();
                    }
                }
            }
        }
    }
}
