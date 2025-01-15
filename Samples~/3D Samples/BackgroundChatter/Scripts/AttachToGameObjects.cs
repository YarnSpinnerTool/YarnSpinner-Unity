#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using System.Collections.Generic;

    [ExecuteAlways]
    public class AttachToGameObjects : MonoBehaviour
    {
        [SerializeField] List<Transform> targets = new List<Transform>();

        public void LateUpdate()
        {
            if (targets.Count == 0)
            {
                return;
            }

            var position = Vector3.zero;
            foreach (var item in targets)
            {
                position += item.position;
            }

            position /= targets.Count;


            this.transform.position = position;
        }
    }
}