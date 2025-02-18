/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/
using UnityEngine;

namespace Yarn.Unity.Samples
{
    // responsible for ensuring that anything the room needed to pull out of the void gets returned so that the user can run the demo again without random props and characters still being there
    // on Start it will grab the existing position and save that, so the spot in the scene is where they return to
    public class VoidReturner : MonoBehaviour
    {
        private Vector3 startingPosition;
        private Quaternion startingAngle;

        void Start()
        {
            startingPosition = this.transform.position;
            startingAngle = this.transform.rotation;
        }

        public void ReturnToTheVoid()
        {
            this.transform.position = startingPosition;
            this.transform.rotation = startingAngle;
        }
    }
}