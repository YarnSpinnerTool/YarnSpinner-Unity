using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Yarn.Unity
{
    public static class ApplicationExitCompat
    {
        private static CancellationTokenSource _exitCts;

        public static CancellationToken ExitCancellationToken
        {
            get
            {
                _exitCts ??= new CancellationTokenSource();

                return _exitCts.Token;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            _exitCts ??= new CancellationTokenSource();

            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    _exitCts.Cancel();
                }
            };
            #endif

            Application.quitting += () => { _exitCts.Cancel(); };
        }
    }

}
