using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace Yarn.Unity
{
    [AttributeUsage(AttributeTargets.Field)]
    public class YarnNodeAttribute : PropertyAttribute
    {
        public string Project { get; set; }

        public YarnNodeAttribute(string project) => Project = project;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(YarnNodeAttribute))]
    public class YarnNodesDrawer : PropertyDrawer
    {
        private string[] GetNodes(YarnProject project)
        {
            return project.YarnProgram.Nodes.Keys.ToArray();
        }

        private YarnProject GetYarnProject(SerializedProperty property)
        {
            var projectFieldName = (attribute as YarnNodeAttribute)?.Project;
            if (projectFieldName == null) { return null; }

            var target = property.serializedObject.FindProperty(projectFieldName).objectReferenceValue;
            return target as YarnProject ?? (target as DialogueRunner)?.yarnProject;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var project = GetYarnProject(property);
            if (project == null || property.propertyType != SerializedPropertyType.String) 
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var nodes = GetNodes(project);
            int index = Array.IndexOf(nodes, property.stringValue);
            int selected = EditorGUI.Popup(
                position,
                new GUIContent(property.displayName, property.tooltip),
                index == -1 ? nodes.Length : index,
                nodes.Select(node => new GUIContent(node)).Append(new GUIContent("None")).ToArray());
            property.stringValue = selected == nodes.Length ? "" : nodes[selected];
        }
    }
#endif
}
