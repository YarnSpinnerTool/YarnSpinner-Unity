using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Interface to prevent play mode if there's errors.
    /// </summary>
    public interface IYarnErrorSource
    {
        internal IList<string> CompileErrors { get; }
    }

    public class YarnPreventPlayMode
    {
        private static YarnPreventPlayMode _instance;
        private static YarnPreventPlayMode Instance => _instance ??= new YarnPreventPlayMode();

        private static readonly Lazy<SceneView> DefaultSceneView = new Lazy<SceneView>(() => EditorWindow.GetWindow<SceneView>());

        /// <summary>
        /// Use this error source to prevent play mode if there are errors.
        /// </summary>
        /// <param name="source">Source to register.</param>
        public static void AddYarnErrorSource(IYarnErrorSource source) => Instance.Add(source);

        private List<WeakReference<IYarnErrorSource>> errorSources = new List<WeakReference<IYarnErrorSource>>();

        private YarnPreventPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void Add(IYarnErrorSource source) => errorSources.Add(new WeakReference<IYarnErrorSource>(source));

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) { return; }

            errorSources.RemoveAll(weakRef => !weakRef.TryGetTarget(out var _));

            bool hasErrors = false;
            HashSet<IYarnErrorSource> seen = new HashSet<IYarnErrorSource>(); 
            foreach (WeakReference<IYarnErrorSource> errorSourceRef in errorSources)
            {
                if (!errorSourceRef.TryGetTarget(out IYarnErrorSource errorSource)) { return; }
                if (errorSource.CompileErrors.Count == 0) { continue; }
                if (seen.Contains(errorSource)) { continue; }

                hasErrors = true;
                seen.Add(errorSource);

                foreach (string error in errorSource.CompileErrors) {
                    Debug.LogError(error);
                }
            }

            if (!hasErrors) { return; }

            EditorApplication.isPlaying = false;
            Debug.LogError("There were import errors. Please fix them to continue.");

            // usually the scene view should be initialized, but if it isn't then it isn't a huge deal
            DefaultSceneView.Value?.ShowNotification(new GUIContent("All Yarn compiler errors must be fixed before entering Play Mode."));
        }
    }
}
