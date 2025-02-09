#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    [ExecuteAlways]
    public class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] new Camera? camera;
        [SerializeField] Transform? target;
        [SerializeField] float speed;
        [SerializeField] Vector3 followOffset;
        [SerializeField] Vector3 lookAtOffset;

        protected void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            transform.position = target.position + followOffset;

            if (camera != null)
            {
                camera.transform.LookAt(target.position + lookAtOffset);
            }
        }

        protected void OnDrawGizmosSelected()
        {
            if (target == null)
            {
                return;
            }

            Gizmos.color = Color.white;

            var lookPosition = target.position + lookAtOffset;
            Gizmos.DrawWireSphere(lookPosition, 0.1f);
            Gizmos.DrawLine(lookPosition, target.position);

        }
    }
}