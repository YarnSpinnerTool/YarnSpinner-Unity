using UnityEngine;

using System;

namespace Yarn.Unity.Samples
{

    [AttributeUsage(AttributeTargets.Field)]
    public class AnimationLayerAttribute : PropertyAttribute
    {
        public AnimationLayerAttribute(string animatorPropertyName)
        {
            this.AnimatorPropertyName = animatorPropertyName;
        }

        public string AnimatorPropertyName { get; }
    }
}