using UnityEngine;

using System;

namespace Yarn.Unity.Samples
{

    [AttributeUsage(AttributeTargets.Field)]
    public class AnimationParameterAttribute : PropertyAttribute
    {
        public AnimationParameterAttribute(string animatorPropertyName)
        {
            this.AnimatorPropertyName = animatorPropertyName;
            this.RequiresSpecificType = false;
        }
        public AnimationParameterAttribute(string animatorPropertyName, AnimatorControllerParameterType type)
        {
            this.AnimatorPropertyName = animatorPropertyName;
            this.RequiresSpecificType = true;
            this.Type = type;
        }

        public string AnimatorPropertyName { get; }
        public bool RequiresSpecificType { get; }
        public AnimatorControllerParameterType Type { get; }
    }
}