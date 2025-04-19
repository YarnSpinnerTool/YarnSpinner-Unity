using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity
{
    public struct OtherType
    {
        public const string ConstNameInOtherType = "other_type_constant";
    }
    public class TestActions : MonoBehaviour
    {
        public const string ConstantFunctionName = "constant_name";

        public void Awake()
        {
            var runner = FindAnyObjectByType<DialogueRunner>();

            if (runner != null)
            {
                // Manually register some functions at run-time. These functions
                // aren't declared as static methods with a YarnFunction
                // attribute. Instead, the action system notices that a call is
                // made to AddFunction, and figures out the name and signature
                // of the function.
                runner.AddFunction("direct_register_lambda_no_params", () => true);
                runner.AddFunction("direct_register_lambda_fixed_params", (int a, int b) => a + b);
                runner.AddFunction("direct_register_lambda_variadic_params", (int[] nums) =>
                {
                    var result = 0;
                    for (int i = 0; i < nums.Length; i++)
                    {
                        result += nums[i];
                    }
                    return result;
                });

                runner.AddFunction("direct_register_method_no_params", DirectRegisterMethodNoParams);
                runner.AddFunction<int, int, int>("direct_register_method_fixed_params", DirectRegisterMethodFixedParams);
                runner.AddFunction<int[], int>("direct_register_method_variadic_params", DirectRegisterMethodVariadicParams);


                const string LocalConstantFunctionName = "local_constant_name";

                runner.AddFunction(LocalConstantFunctionName, () => true);
                runner.AddFunction(ConstantFunctionName, () => true);
                runner.AddFunction(OtherType.ConstNameInOtherType, () => true);
            }
        }

        private bool DirectRegisterMethodNoParams() => true;
        private int DirectRegisterMethodFixedParams(int a, int b) => a + b;
        private int DirectRegisterMethodVariadicParams(params int[] nums)
        {
            var result = 0;
            for (int i = 0; i < nums.Length; i++)
            {
                result += nums[i];
            }
            return result;
        }

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

        [YarnFunction("int_void")]
        public static int DemoFunction1()
        {
            Debug.Log($"Demo function {nameof(DemoFunction1)}");
            return 1;
        }

        [YarnFunction("int_params")]
        public static int DemoFunction2(int input)
        {
            Debug.Log($"Demo function {nameof(DemoFunction2)}");
            return 1;
        }

        [YarnCommand("instance_variadic")]
        public void VariadicInstanceFunction(int required, params bool[] bools)
        {
            Debug.Log($"Variadic instance function: {required}, ({string.Join(", ", bools)})");
        }

        [YarnCommand("static_variadic")]
        public void VariadicStaticFunction(int required, params bool[] bools)
        {
            Debug.Log($"Variadic static function: {required}, ({string.Join(", ", bools)})");
        }
    }
}
