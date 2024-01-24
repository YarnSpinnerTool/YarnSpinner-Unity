/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

namespace Yarn.Unity.Example
{
    /// <summary>
    /// Attached to the non-player characters, and stores the name of the Yarn
    /// node that should be run when you talk to them.
    /// </summary>
    public class NPC : MonoBehaviour
    {
        public string characterName = "";

        public string talkToNode = "";
    }

}
