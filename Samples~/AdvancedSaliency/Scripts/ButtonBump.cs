using UnityEngine;

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
