using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
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
            if (!targetObject is TContainedType)
            {
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
            }
            else
            {
                return;
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
