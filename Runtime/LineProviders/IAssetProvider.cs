/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Contains methods for accessing assets of a given type stored within an
    /// object.
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// Attempts to fetch an asset of type <typeparamref name="T"/> from the
        /// object.
        /// </summary>
        /// <typeparam name="T">The type of the assets.</typeparam>
        /// <param name="result">On return, the fetched asset, or <see
        /// langword="null"/>.</param>
        /// <returns><see langword="true"/> if an asset was fetched; <see
        /// langword="false"/> otherwise.</returns>
        public bool TryGetAsset<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? result) where T : UnityEngine.Object;

        /// <summary>
        /// Gets a collection of assets of type <typeparamref name="T"/> from
        /// the target.
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <returns>A collection of assets. This collection may be
        /// empty.</returns>
        public IEnumerable<T> GetAssetsOfType<T>() where T : UnityEngine.Object;
    }

}
