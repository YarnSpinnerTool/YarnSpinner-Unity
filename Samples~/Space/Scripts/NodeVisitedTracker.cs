using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Field ... is never assigned to and will always have its default value
// null
#pragma warning disable 0649

namespace Yarn.Unity.Example
{
    public class NodeVisitedTracker : MonoBehaviour
    {

        // The dialogue runner that we want to attach the 'visited'
        // function to
#pragma warning disable 0649
        [SerializeField] Yarn.Unity.DialogueRunner dialogueRunner;
#pragma warning restore 0649

        // a HashSet is like a List or Array except it can't guarantee the order of items 
        // which is OK if you don't care about order and just want to search inside it, like we do here!
        // but you could replace this List<string> if you wanted to log the precise order of visited nodes, etc.
        private HashSet<string> _visitedNodes = new HashSet<string>();

        // we call AddFunction in Awake instead of Start, to ensure the Yarn function "visited()" 
        // is added to the Dialogue Runner before it starts automatically (if configured to start automatically)
        void Awake()
        {
            // Register a function on startup called "visited" that lets
            // Yarn scripts query to see if a node has been run before.
            dialogueRunner.AddFunction("visited", delegate (string nodeName)
            {
                return _visitedNodes.Contains(nodeName);
            });

        }

        // Called by the Dialogue Runner to notify us that a node finished
        // running. 
        public void NodeComplete(string nodeName)
        {
            // Log that the node has been run.
            _visitedNodes.Add(nodeName);
        }


        // Called by the Dialogue Runner to notify us that a new node
        // started running. 
        public void NodeStart(string nodeName)
        {
            // Log that the node has been run.

            var tags = new List<string>(dialogueRunner.GetTagsForNode(nodeName));

            Debug.Log($"Starting the execution of node {nodeName} with {tags.Count} tags.");
        }

    }
}
