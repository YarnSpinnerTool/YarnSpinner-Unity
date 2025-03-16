/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

#if USE_TMP
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    public class MouthView : MonoBehaviour
    {
        [SerializeField] string? characterName;
        public string? CharacterName => characterName;

        public void SetOverride(Texture2D deathMouthTexture)
        {
            this.overrideTexture = deathMouthTexture;

            SetMouthTexture(deathMouthTexture);

        }
        public void ClearOverride()
        {
            this.overrideTexture = null;
            if (this.currentFacialExpression != null
                && this.currentFacialExpression.TryGetTexture(LipSyncedVoiceLine.MouthShape.X, out var defaultClosedMouthTexture))
            {
                SetMouthTexture(defaultClosedMouthTexture);
            }
        }

        private Texture2D? overrideTexture;

        [SerializeField] new Renderer? renderer;
        private MaterialPropertyBlock? propertyBlock;

        LipSyncTextureGroup? currentFacialExpression;
        [SerializeField] SerializableDictionary<string, LipSyncTextureGroup> facialExpressions = new();
        [SerializeField] LipSyncTextureGroup? defaultFacialExpression;

        [YarnCommand("expression")]
        public void SetFacialExpression(string expression)
        {
            if (!facialExpressions.TryGetValue(expression, out currentFacialExpression))
            {
                Debug.LogError($"Unknown facial expression {expression}; expected {string.Join(", ", facialExpressions.Keys)}", this);
            }
        }

        public void Awake()
        {
            propertyBlock = new();

            currentFacialExpression = defaultFacialExpression;

            SetMouthShape(LipSyncedVoiceLine.MouthShape.X);
        }

        public void SetMouthShape(LipSyncedVoiceLine.MouthShape mouthShape)
        {
            if (overrideTexture != null)
            {
                // Our mouth texture is being overridden; do nothing
                return;
            }

            if (renderer == null)
            {
                return;
            }

            if (currentFacialExpression == null)
            {
                Debug.LogWarning($"No facial expression set", this);
                return;
            }

            if (currentFacialExpression.TryGetTexture(mouthShape, out var texture))
            {
                SetMouthTexture(texture);
            }
            else
            {
                Debug.LogWarning($"No mouth shape {mouthShape}", this);
            }
        }

        private void SetMouthTexture(Texture2D texture)
        {
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            propertyBlock.SetTexture("_Texture", texture);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
