using UnityEngine;

#nullable enable

namespace Yarn.Unity.QuestGraphs
{
    [System.Serializable]
    public struct SerializableQuestEdge
    {
        [SerializeField] string fromNodeDescriptor;
        [SerializeField] string toNodeDescriptor;
        [SerializeField] string? requirement;
        [SerializeField] string? description;

        public static implicit operator QuestGraphEdgeDescriptor(SerializableQuestEdge data)
        {
            return data.Descriptor;
        }

        public static implicit operator SerializableQuestEdge(QuestGraphEdgeDescriptor descriptor)
        {
            return new SerializableQuestEdge
            {
                requirement = descriptor.Requirement,
                description = descriptor.Description,
                fromNodeDescriptor = descriptor.FromNode,
                toNodeDescriptor = descriptor.ToNode
            };
        }

        public readonly QuestGraphEdgeDescriptor Descriptor
        {
            get
            {
                return new QuestGraphEdgeDescriptor(this.fromNodeDescriptor, this.toNodeDescriptor, this.requirement, this.description);
            }
        }

        public bool IsValid
        {
            get
            {
                return QuestGraphNodeDescriptor.CanParse(fromNodeDescriptor)
                && QuestGraphNodeDescriptor.CanParse(toNodeDescriptor);
            }
        }
    }
}
