#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Yarn.Unity;

    public class SimplePath : MonoBehaviour
    {
        [System.Serializable]
        public struct Position
        {
            public Vector3 position;
            public float delay;
        }

        public List<Position> pathElements = new();

        public int Count => pathElements.Count;

        public Position GetPositionData(int i)
        {

            if (i < 0 || i > pathElements.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(i));
            }
            return pathElements[i];
        }

        public Vector3 GetWorldPosition(int i)
        {
            return transform.TransformPoint(GetPositionData(i).position);
        }

        public float GetDelay(int i)
        {
            return GetPositionData(i).delay;
        }

    }
}