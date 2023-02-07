using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity
{
    public class TestActions : MonoBehaviour
    {
        [YarnCommand]
        public void InstanceDemoActionWithNoName()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("instance_demo_action")]
        public void InstanceDemoAction()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("instance_demo_action_with_params")]
        public void InstanceDemoAction(int param)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand("instance_demo_action_with_optional_params")]
        public void InstanceDemoAction(int param, int param2 = 0)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand]
        public static void StaticDemoActionWithNoName()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("static_demo_action")]
        public static void StaticDemoAction()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("static_demo_action_with_params")]
        public static void StaticDemoAction(int param)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand("static_demo_action_with_optional_params")]
        public static void StaticDemoAction(int param, int param2 = 0)
        {
            Debug.Log($"Demo action: {param}!");
        }
    }
}
