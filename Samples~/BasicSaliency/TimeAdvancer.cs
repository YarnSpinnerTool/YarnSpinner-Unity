using UnityEngine;
using TMPro;

namespace Yarn.Unity.Samples
{
    public class TimeAdvancer : MonoBehaviour
    {
        public BasicSaliencyVariableStorage variableStore;
        public TMP_Text label;

        private void AdvanceTime()
        {
            // if it is morning we move to evening and stop there
            if (variableStore.Time == TimeOfDay.Morning)
            {
                variableStore.Time = TimeOfDay.Evening;
                return;
            }

            // it is evening so we need to advance the day AND set it to be morning
            // wednesday wraps around to monday
            variableStore.Time = TimeOfDay.Morning;
            switch (variableStore.Day)
            {
                case Day.Monday:
                    variableStore.Day = Day.Tuesday;
                    break;
                case Day.Tuesday:
                    variableStore.Day = Day.Wednesday;
                    break;
                case Day.Wednesday:
                    variableStore.Day = Day.Monday;
                    break;
            }
        }
        private void UpdateLabel()
        {
            string day = "Monday";
            switch (variableStore.Day)
            {
                case Day.Tuesday:
                    day = "Tuesday";
                    break;
                case Day.Wednesday:
                    day = "Wednesday";
                    break;
            }
            
            string time = "evening";
            if (variableStore.Time == TimeOfDay.Morning)
            {
                time = "morning";
            }
            
            label.text = $"It is {day} {time}.";
        }

        void Start()
        {
            UpdateLabel();
        }

        void OnTriggerEnter(Collider other)
        {
            AdvanceTime();
            UpdateLabel();
            Debug.Log("updated");
        }
    }
}