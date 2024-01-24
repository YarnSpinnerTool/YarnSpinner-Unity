/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using System.Collections;

namespace Yarn.Unity.Example {

    /// <summary>
    /// Control the position of the camera and its behaviour
    /// </summary>
    /// <remarks>
    /// Camera should have <see cref="minPosition"/> and <see cref="maxPosition"/> of the same because we're
    /// dealing with 2D. The movement speed shouldn't be too fast nor too slow
    /// </remarks>
    public class CameraFollow : MonoBehaviour {

        /// <summary>
        /// Target of the camera
        /// </summary>
        public Transform target;

        /// <summary>
        /// Minimum position of camera
        /// </summary>
        public float minPosition = -5.3f;

        /// <summary>
        /// Maximum position of camera
        /// </summary>
        public float maxPosition = 5.3f;

        /// <summary>
        /// Movement speed of camera
        /// </summary>
        public float moveSpeed = 1.0f;

        // Update is called once per frame
        void Update () {
            if (target == null) {
                return;
            }
            var newPosition = Vector3.Lerp(transform.position, target.position, moveSpeed * Time.deltaTime);

            newPosition.x = Mathf.Clamp(newPosition.x, minPosition, maxPosition);
            newPosition.y = transform.position.y;
            newPosition.z = transform.position.z;

            transform.position = newPosition;
        }
    }
}

