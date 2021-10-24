using System;
using System.Collections.Generic;
using System.Linq;
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

        internal bool Destroyed { get; }
    }

    public class YarnPreventPlayMode
    {
        /// <summary>
        /// Comparer to proxy comparison to objects themselves.
        /// </summary>
        /// <typeparam name="T">Passed directly to <see cref="WeakReference{T}"/></typeparam>
        private class WeakRefComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
        {
            public bool Equals(WeakReference<T> x, WeakReference<T> y)
                => x.TryGetTarget(out T xVal) && y.TryGetTarget(out T yVal) && ReferenceEquals(xVal, yVal);

            public int GetHashCode(WeakReference<T> obj)
                => obj.TryGetTarget(out var result) ? result.GetHashCode() : 0;
        }

        private static YarnPreventPlayMode _instance;
        private static YarnPreventPlayMode Instance => _instance ??= new YarnPreventPlayMode();

        /// <summary>
        /// Register a error source type to gather initial asset state.
        /// 
        /// Note that you may have to use <see cref="InitializeOnLoadAttribute"/> on your class to get it to reliably register
        /// on domain reloads etc, as by default .NET lazily loads static state.
        /// </summary>
        /// <typeparam name="T">An asset importer type that qualifies as error source.</typeparam>
        /// <param name="filterQuery">Search query (see <see cref="AssetDatabase.FindAssets(string)"/> documentation for formatting).</param>
        public static void AddYarnErrorSourceType<T>(string filterQuery) where T : AssetImporter, IYarnErrorSource
            => Instance.assetSearchQueries.Enqueue((importer => importer as T, filterQuery));

        /// <summary>
        /// Use this error source to prevent play mode if there are errors.
        /// </summary>
        /// <param name="source">Source to register.</param>
        public static void AddYarnErrorSource(IYarnErrorSource source) 
            => Instance.errorSources.Add(new WeakReference<IYarnErrorSource>(source));

        public static bool HasCompileErrors() => Instance.CompilerErrors().Any();

        private readonly Queue<(Func<AssetImporter, IYarnErrorSource> converter, string filterQuery)> assetSearchQueries =
            new Queue<(Func<AssetImporter, IYarnErrorSource> converter, string filterQuery)>();

        private readonly HashSet<WeakReference<IYarnErrorSource>> errorSources = 
            new HashSet<WeakReference<IYarnErrorSource>>(new WeakRefComparer<IYarnErrorSource>());

        private readonly HashSet<string> deletedSources = new HashSet<string>();

        private YarnPreventPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) { return; }

            bool isValid = true;
            foreach (string error in CompilerErrors())
            {
                isValid = false;
                Debug.Log(error);
            }

            if (isValid) { return; }

            EditorApplication.isPlaying = false;
            Debug.LogError("There were import errors. Please fix them to continue.");

            // usually the scene view should be initialized, but if it isn't then it isn't a huge deal
            EditorWindow.GetWindow<SceneView>()
                ?.ShowNotification(new GUIContent("All Yarn compiler errors must be fixed before entering Play Mode."));
        }

        private IEnumerable<string> CompilerErrors()
        {
            // delete expired weak refs
            errorSources.RemoveWhere(weakRef => !weakRef.TryGetTarget(out var source) || source.Destroyed);

            // import all unloaded assets
            while (assetSearchQueries.Count > 0)
            {
                (Func<AssetImporter, IYarnErrorSource> converter, string filterQuery) = assetSearchQueries.Dequeue();

                errorSources.UnionWith(AssetDatabase.FindAssets(filterQuery)
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => AssetImporter.GetAtPath(path))
                    .Select(converter)
                    .Where(source => source != null)
                    .Select(source => new WeakReference<IYarnErrorSource>(source)));
            }

            foreach (WeakReference<IYarnErrorSource> errorSourceRef in errorSources)
            {
                if (!errorSourceRef.TryGetTarget(out IYarnErrorSource errorSource)) { continue; }
                if (errorSource.CompileErrors.Count == 0) { continue; }

                foreach (string error in errorSource.CompileErrors)
                {
                    yield return error;
                }
            }
        }
    }
}
