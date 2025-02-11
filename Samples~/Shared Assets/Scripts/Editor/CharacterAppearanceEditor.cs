using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yarn.Unity.Editor;

namespace Yarn.Unity.Samples.Editor
{
    [CustomEditor(typeof(CharacterAppearance))]
    public class CharacterAppearanceEditor : YarnEditor
    {

        private struct Preset
        {
            public Color baseColor;
            public Color fadeColor;
            public Preset(string baseColorHex, string fadeColorHex)
            {
                if (!ColorUtility.TryParseHtmlString(baseColorHex, out baseColor))
                {
                    baseColor = Color.magenta;
                }
                if (!ColorUtility.TryParseHtmlString(fadeColorHex, out fadeColor))
                {
                    fadeColor = Color.magenta;
                }
            }
        }

        private Dictionary<string, Preset> presets = new()
        {
            {"Blue", new("#5756BD", "#6794D9")},
            {"Purple", new("#5C39B8", "#A676E7")},
            {"Sky Blue", new("#6794D9", "#CCE5FE")},
            {"Red", new("#CF534F", "#FE6C40")},
            {"Orange", new("#FF7244", "#FF9D44")},
            {"Yellow", new("#FF952F", "#FFD565")},
            {"Green", new("#1B8469", "#5EC889")},
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var modeProperty = serializedObject.FindProperty("appearanceMode");
            var baseColorProperty = serializedObject.FindProperty("baseColor");
            var fadeColorProperty = serializedObject.FindProperty("fadeColor");

            foreach (var preset in presets)
            {
                if (GUILayout.Button(preset.Key))
                {
                    Undo.RecordObject(target, "Set character appearance to " + preset.Key);
                    modeProperty.enumValueIndex = 1;
                    baseColorProperty.colorValue = preset.Value.baseColor;
                    fadeColorProperty.colorValue = preset.Value.fadeColor;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}