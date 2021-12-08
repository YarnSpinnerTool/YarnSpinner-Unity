using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yarn.Unity.Editor
{
    using TypeRegistrationQuery = ValueTuple<Func<AssetImporter, IYarnErrorSource>, string>;
    using ErrorSourceSet = HashSet<WeakReference<IYarnErrorSource>>;

    /// <summary>
    /// Interface to prevent play mode if there's errors.
    /// </summary>
    public interface IYarnErrorSource
    {
#if UNITY_2020_3_OR_NEWER
        internal
#endif
        IList<string> CompileErrors { get; }

#if UNITY_2020_3_OR_NEWER
        internal
#endif
        bool Destroyed { get; }
    }

    public class YarnPreventPlayMode
    {
        /// <summary>
        /// Comparer to proxy comparison to objects themselves.
        /// </summary>
        /// <typeparam name="T">Passed directly to <see
        /// cref="WeakReference{T}"/></typeparam>
        private class WeakRefComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
        {
            public bool Equals(WeakReference<T> x, WeakReference<T> y)
                => x.TryGetTarget(out T xVal) && y.TryGetTarget(out T yVal) && ReferenceEquals(xVal, yVal);

            public int GetHashCode(WeakReference<T> obj)
                => obj.TryGetTarget(out var result) ? result.GetHashCode() : 0;
        }

        private static YarnPreventPlayMode _instance;
        private static YarnPreventPlayMode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new YarnPreventPlayMode();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Register a error source type to gather initial asset state.
        ///
        /// Note that you may have to use <see
        /// cref="InitializeOnLoadAttribute"/> on your class to get it to
        /// reliably register on domain reloads etc, as by default .NET
        /// lazily loads static state.
        /// </summary>
        /// <typeparam name="T">An asset importer type that qualifies as
        /// error source.</typeparam>
        /// <param name="filterQuery">Search query (see <see
        /// cref="AssetDatabase.FindAssets(string)"/> documentation for
        /// formatting).</param>
        public static void AddYarnErrorSourceType<T>(string filterQuery) where T : AssetImporter, IYarnErrorSource
            => Instance.assetSearchQueries.Enqueue((importer => importer as T, filterQuery));

        /// <summary>
        /// Use this error source to prevent play mode if there are errors.
        /// </summary>
        /// <param name="source">Source to register.</param>
        public static void AddYarnErrorSource(IYarnErrorSource source)
            => Instance.errorSources.Add(new WeakReference<IYarnErrorSource>(source));

        public static bool HasCompileErrors() => Instance.CompilerErrors().Any();

        private readonly Queue<TypeRegistrationQuery> assetSearchQueries = new Queue<TypeRegistrationQuery>();

        private readonly ErrorSourceSet errorSources = new ErrorSourceSet(new WeakRefComparer<IYarnErrorSource>());

        private YarnPreventPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) { return; }

            bool isValid = true;
            foreach (string error in CompilerErrors())
            {
                isValid = false;
                Debug.LogError(error);
            }

            if (isValid) { return; }

            EditorApplication.isPlaying = false;
            Debug.LogError("There were import errors. Please fix them to continue.");

            // usually the scene view should be initialized, but if it
            // isn't then it isn't a huge deal
            SceneView sceneView = EditorWindow.GetWindow<SceneView>();

            if (sceneView != null) {
                sceneView.ShowNotification(new GUIContent("All Yarn compiler errors must be fixed before entering Play Mode."));
            }
        }

        private IEnumerable<string> CompilerErrors()
        {
            // delete expired weak refs
            errorSources.RemoveWhere(weakRef => !weakRef.TryGetTarget(out var source) || source.Destroyed);

            // import all unloaded assets
            while (assetSearchQueries.Count > 0)
            {
                (var converter, string filterQuery) = assetSearchQueries.Dequeue();

                errorSources.UnionWith(
                    YarnEditorUtility.GetAllAssetsOf(filterQuery, converter)
                        .Select(source => new WeakReference<IYarnErrorSource>(source)));
            }

            return errorSources
                .Select(errorRef => errorRef.TryGetTarget(out IYarnErrorSource errorSource) ? errorSource : null)
                .Where(errorSource => errorSource != null && errorSource.CompileErrors.Count > 0)
                .SelectMany(errorSource => errorSource.CompileErrors);
        }
    }
}
