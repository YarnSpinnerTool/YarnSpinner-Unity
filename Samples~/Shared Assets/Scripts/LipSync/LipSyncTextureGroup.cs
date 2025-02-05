using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Samples
{
    [CreateAssetMenu]
    public class LipSyncTextureGroup : ScriptableObject
    {
        [SerializeField] SerializableDictionary<LipSyncedVoiceLine.MouthShape, Texture2D> textures = new();

        public bool TryGetTexture(LipSyncedVoiceLine.MouthShape shape, out Texture2D texture)
        {
            return textures.TryGetValue(shape, out texture);
        }

    }
}