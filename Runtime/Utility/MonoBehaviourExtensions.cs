#nullable enable
using System.Threading;
using UnityEngine;

namespace Yarn.Unity
{
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Gets a CancellationToken that is cancelled when the MonoBehaviour's GameObject is destroyed.
        /// </summary>
        public static CancellationToken GetDestroyCancellationToken(this MonoBehaviour mb)
        {
            #if UNITY_2022_2_OR_NEWER
            return mb.destroyCancellationToken;
            #endif

            DestroyTokenNotifier notifier = mb.gameObject.GetComponent<DestroyTokenNotifier>()
                                            ?? mb.gameObject.AddComponent<DestroyTokenNotifier>();

            return notifier.Cts.Token;
        }
    }
    
    internal class DestroyTokenNotifier : MonoBehaviour
    {
        public CancellationTokenSource Cts { get; } = new();

        private void OnDestroy()
        {
            Cts.Cancel();
            Cts.Dispose();
        }
    }
}
