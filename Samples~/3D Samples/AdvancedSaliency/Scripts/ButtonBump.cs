/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

namespace Yarn.Unity.Samples
{
    public class ButtonBump : MonoBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            var updater = other.GetComponent<Yarn.Unity.Samples.ValueUpdater>();
            if (updater == null)
            {
                return;
            }
            updater.UpdateValue();
        }
    }
}