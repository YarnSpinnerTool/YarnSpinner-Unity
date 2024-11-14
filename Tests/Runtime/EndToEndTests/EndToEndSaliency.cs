/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Linq;
using UnityEngine;
using Yarn.Saliency;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Yarn.Unity.Tests
{
    public class EndToEndSaliency : MonoBehaviour
    {
        IContentSaliencyStrategy saliency;

        [SerializeField] UnityEngine.UI.Text debugLabel;

        DialogueRunner dialogueRunner;

        public void Awake()
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        public void Update()
        {
            debugLabel.text = $"Saliency: " + (dialogueRunner.Dialogue.ContentSaliencyStrategy?.GetType().Name ?? "(default)");
        }

        [YarnCommand("set_saliency")]
        public static void SetSaliency(string saliencyType)
        {
            var instance = FindAnyObjectByType<EndToEndSaliency>();
            var storage = FindAnyObjectByType<VariableStorageBehaviour>();

            switch (saliencyType)
            {
                case "first":
                    instance.saliency = new FirstSaliencyStrategy();
                    break;
                case "best":
                    instance.saliency = new BestSaliencyStrategy();
                    break;
                case "best-least-recent":
                    instance.saliency = new BestLeastRecentlyViewedSalienceStrategy(storage);
                    break;
                case "random-best-least-recent":
                    instance.saliency = new RandomBestLeastRecentlyViewedSalienceStrategy(storage);
                    break;
                default:
                    Debug.LogError("Unknown saliency strategy " + saliencyType);
                    return;
            }

            Debug.Log("Changing saliency strategy to " + instance.saliency.GetType().Name);

            instance.dialogueRunner.Dialogue.ContentSaliencyStrategy = instance.saliency;
        }

    }
}
