using UnityEngine;

namespace Yarn.Unity.Example
{
    /// <summary>Simple utility script for spinning an object, used to demo a feature in YarnSpinner 3D sample scene.</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 spinDegreesPerSecond = new Vector3(0f, 90f, 0f);

        void Update()
        {
            transform.Rotate(spinDegreesPerSecond * Time.deltaTime);
        }
    }
}
