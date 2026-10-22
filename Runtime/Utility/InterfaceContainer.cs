/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// This type exists to allow connecting up interfaces into objects via the inspector.
    /// </summary>
    /// <remarks>
    /// This is a convenience mechanism because there is no practical way to serialise an interface in Unity.
    /// Your interfaces do still need to be something that is a subclass of <c>UnityEngine.Object</c> for it to be serialisable using this wrapper.
    /// Essentially this is just a wrapper around a monobehaviour but allows you to think only in the terms of the interface.
    /// Which is generally more useful and helpful.
    /// </remarks>
    /// <typeparam name="TContainedType">The interface type to be contained, it must be a unity object.</typeparam>
    [System.Serializable]
    public class InterfaceContainer<TContainedType> : ISerializationCallbackReceiver where TContainedType : class
    {
        public UnityEngine.Object? targetObject;
        public TContainedType? Interface
        {
            get
            {
                return targetObject as TContainedType;
            }
        }

        public static implicit operator TContainedType?(InterfaceContainer<TContainedType> value)
        {
            return value.Interface;
        }

        // basically if we find a component that is our interface we override the target to be that
        // and otherwise we null it out, also wiping the connection in the inspector
        void OnValidate()
        {
            if (targetObject == null)
            {
                return;
            }
            if (this.Interface != null)
            {
                return;
            }

            if (targetObject is GameObject gameObject)
            {
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component is TContainedType)
                    {
                        targetObject = component;
                        return;
                    }
                }
            }

            targetObject = null;
        }

        public void OnBeforeSerialize()
        {
            OnValidate();
        }

        public void OnAfterDeserialize()
        {
            return;
        }
    }
}
