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
    }
}