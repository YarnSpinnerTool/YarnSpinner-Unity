using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yarn;

#nullable enable

namespace Yarn.Unity.QuestGraphs
{

    [CreateAssetMenu]
    public class QuestGraphData : ScriptableObject
    {
        [SerializeField] internal List<SerializableQuestEdge> edges = new();

        internal static QuestGraphData Create(IEnumerable<QuestGraphEdgeDescriptor> edgeDescriptors)
        {
            var data = ScriptableObject.CreateInstance<QuestGraphData>();

            foreach (var edge in edgeDescriptors)
            {
                data.edges.Add(edge);
            }

            return data;

        }


        public IEnumerable<SerializableQuestEdge> Edges => edges;

        public static IEnumerable<QuestGraphNodeDescriptor> GetTopologicalSort(IEnumerable<QuestGraphEdgeDescriptor> edges)
        {
            Dictionary<QuestGraphNodeDescriptor, List<QuestGraphNodeDescriptor>> outEdges = new();

            foreach (QuestGraphEdgeDescriptor edge in edges)
            {
                if (outEdges.TryGetValue(edge.FromNode, out var outEdgesForNode) == false)
                {
                    outEdgesForNode = new();
                    outEdges[edge.FromNode] = outEdgesForNode;
                }
                outEdgesForNode.Add(edge.ToNode);
            }

            HashSet<QuestGraphNodeDescriptor> unvisited = edges.SelectMany(e => new[] { e.FromNode, e.ToNode }).Distinct().ToHashSet();
            HashSet<QuestGraphNodeDescriptor> visited = new();
            HashSet<QuestGraphNodeDescriptor> visiting = new();

            List<QuestGraphNodeDescriptor> result = new();

            while (unvisited.Any())
            {
                var current = unvisited.First();
                unvisited.Remove(current);

                Visit(current);
            }

            void Visit(QuestGraphNodeDescriptor node)
            {
                if (visited.Contains(node))
                {
                    return;
                }
                if (visiting.Contains(node))
                {
                    throw new System.InvalidOperationException("Can't get a topological sort: graph contains a cycle");
                }

                visiting.Add(node);

                if (outEdges.TryGetValue(node, out var outEdgesForNode))
                {
                    foreach (var outNode in outEdges[node])
                    {
                        Visit(outNode);
                    }
                }

                visited.Add(node);

                result.Add(node);
            }

            result.Reverse();

            return result;
        }
    }

#if  UNITY_EDITOR
    namespace Editor
    {
        using UnityEditor;

        [CustomEditor(typeof(QuestGraphData))]
        public class QuestGraphDataEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                if (target is not QuestGraphData graphData)
                {
                    return;
                }
                foreach (var edge in graphData.edges)
                {
                    if (edge.IsValid)
                    {
                        QuestGraphEdgeDescriptor edgeDescriptor = edge;
                        EditorGUILayout.LabelField($"{edgeDescriptor.FromNode} -- {edgeDescriptor.ToNode}");
                    }
                }
            }
        }

    }
#endif


}
