using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity.Samples
{

    [ExecuteAlways]
    public class CharacterAppearance : MonoBehaviour
    {
        public enum Mode
        {
            Simple, Complex
        };

        [SerializeField] Mode appearanceMode = Mode.Simple;
        [SerializeField] new Renderer? renderer;

        [ShowIf(nameof(appearanceMode), Mode.Complex)]
        [SerializeField] Color fadeColor = Color.red;
        [SerializeField] Color baseColor = Color.yellow;


        const float simpleTintSaturationOffset = -0.3f;
        const float simpleTintValueOffset = 0.25f;

        private MaterialPropertyBlock? materialPropertyBlock;
        private void ApplyAppearance()
        {
            if (renderer == null)
            {
                renderer = GetComponentInChildren<Renderer>();
            }

            if (renderer == null)
            {
                return;
            }

            if (materialPropertyBlock == null)
            {
                materialPropertyBlock = new MaterialPropertyBlock();
            }

            if (this.appearanceMode == Mode.Simple)
            {
                fadeColor = baseColor;
                Color.RGBToHSV(fadeColor, out var hue, out var sat, out var value);
                sat += simpleTintSaturationOffset;
                value += simpleTintValueOffset;
                fadeColor = Color.HSVToRGB(hue, sat, value);
            }

            materialPropertyBlock.SetColor("_Top_Color", fadeColor);
            materialPropertyBlock.SetColor("_Bottom_Color", baseColor);

            renderer.SetPropertyBlock(materialPropertyBlock);
        }

        protected void OnEnable()
        {
            ApplyAppearance();
        }

        protected void OnValidate()
        {
            ApplyAppearance();
        }

        protected void Reset()
        {
            ApplyAppearance();
        }
    }

}