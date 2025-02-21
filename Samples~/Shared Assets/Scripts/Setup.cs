#nullable enable

namespace Yarn.Unity.Samples
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;



    public static class Setup
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            var dialogueRunner = Object.FindAnyObjectByType<DialogueRunner>();
            if (dialogueRunner == null)
            {
                return;
            }

            dialogueRunner.Dialogue.ContentSaliencyStrategy = new Yarn.Saliency.RandomBestLeastRecentlyViewedSaliencyStrategy(dialogueRunner.VariableStorage);
        }
    }
}