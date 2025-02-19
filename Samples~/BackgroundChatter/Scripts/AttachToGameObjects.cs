/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using System.Collections.Generic;

    [ExecuteAlways]
    public class AttachToGameObjects : MonoBehaviour
    {
        [SerializeField] List<Transform?> targets = new List<Transform?>();

        public void LateUpdate()
        {
            if (targets.Count == 0)
            {
                return;
            }

            var position = Vector3.zero;
            var count = 0;
            foreach (var item in targets)
            {
                if (item != null)
                {
                    position += item.position;
                    count += 1;
                }
            }

            if (count > 0)
            {
                position /= count;
            }

            this.transform.position = position;
        }
    }
}