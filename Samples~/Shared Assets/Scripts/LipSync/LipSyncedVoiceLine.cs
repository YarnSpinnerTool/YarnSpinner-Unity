using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class LipSyncedVoiceLine : ScriptableObject, Yarn.Unity.IAssetProvider
    {
        public enum MouthShape
        {
            // closed mouth: M P B
            A,
            // slight open mouth with teeth: K S T
            B,
            // open mouth: most vowels
            C,
            // wide open mouth: "ah"
            D,
            // slight rounded mouth: "er"
            E,
            // puckered lips: "oo"
            F,
            // labiodental: "ff"
            G,
            // alveolar: "l"
            H,
            // mouth closed, silent
            X
        }

        [Serializable]
        public struct MouthShapeFrame
        {
            public MouthShape mouthShape;
            public float time;
        }

        public AudioClip? audioClip;

        public List<MouthShapeFrame> frames = new();

        public float Duration
        {
            get
            {
                if (frames == null || frames.Count == 0)
                {
                    return 0f;
                }
                return frames[^1].time;
            }
        }

        public MouthShape Evaluate(float time)
        {
            if (frames == null || frames.Count == 0)
            {
                // We have no frames; return the closed mouth shape.
                return MouthShape.X;
            }

            if (time <= 0)
            {
                // We were asked for the start (or a negative time); return the
                // first mouth shape.
                return frames[0].mouthShape;
            }

            // Walk through the frames and find the first one starts that after our time
            // index
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                if (time < frame.time)
                {
                    return frame.mouthShape;
                }
            }

            // We fell off the list; return the last shape
            return frames[^1].mouthShape;
        }

        public bool TryGetAsset<T>([NotNullWhen(true)] out T? result) where T : UnityEngine.Object
        {
            if (typeof(T).IsAssignableFrom(typeof(LipSyncedVoiceLine)))
            {
                result = (T)(object)this;
                return true;
            }

            if (typeof(T).IsAssignableFrom(typeof(AudioClip)))
            {
                if (audioClip != null)
                {
                    result = (T)(object)audioClip;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public IEnumerable<T> GetAssetsOfType<T>() where T : UnityEngine.Object
        {
            if (TryGetAsset(out T? result))
            {
                return new[] { result };
            }
            else
            {
                return Array.Empty<T>();
            }
        }
    }
}