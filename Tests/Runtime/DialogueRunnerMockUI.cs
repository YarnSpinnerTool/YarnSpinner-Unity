using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Tests
{
    public class DialogueRunnerMockUI : Yarn.Unity.DialogueViewBase
    {
        private static DialogueRunnerMockUI instance;
        public static DialogueRunnerMockUI GetInstance(string name)
        {
            return name == "custom" ? instance : null;
        }

        // The text of the most recently received line that we've been
        // given
        public string CurrentLine { get; private set; } = default;

        // The text of the most recently received options that we've ben
        // given
        public List<string> CurrentOptions { get; private set; } = new List<string>();

        // The amount of time that this view will take before notifying
        // that the line has been delivered. If zero, lines are delivered
        // immediately.
        public float simulatedLineDeliveryTime = 0;

        // The amount of time that this view will take before notifying
        // that the line has been dismissed. If zero, lines are dismissed
        // immediately.
        public float simulatedLineDismissalTime = 0;

        // If true, the line has been interrupted, and we should notify
        // that delivery is complete immediately.
        private bool isLineInterrupted;

        private void Awake()
        {
            instance = this;
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onLineDeliveryComplete)
        {
            // Store the localised text in our CurrentLine property and
            // signal that we're done "delivering" the line after the
            // correct amount of time
            CurrentLine = dialogueLine.Text.Text;

            isLineInterrupted = false;

            if (simulatedLineDeliveryTime > 0)
            {
                StartCoroutine(SimulateLineDelivery(onLineDeliveryComplete));
            }
            else
            {
                onLineDeliveryComplete();
            }
        }

        private IEnumerator SimulateLineDelivery(Action onLineDeliveryComplete)
        {
            // Wait for an amount of time before calling the completion
            // handler
            var lineDeliveryTimeRemaining = simulatedLineDeliveryTime;

            while (lineDeliveryTimeRemaining > 0)
            {
                if (isLineInterrupted)
                {
                    break;
                }
                yield return null;
            }

            onLineDeliveryComplete();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            CurrentOptions.Clear();
            foreach (var option in dialogueOptions)
            {
                CurrentOptions.Add(option.Line.Text.Text);
            }
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            // Signal that we're done "dismissing" the line after the
            // correct amount of time

            if (simulatedLineDeliveryTime > 0)
            {
                StartCoroutine(SimulateLineDismissal(onDismissalComplete));
            }
            else
            {
                onDismissalComplete();
            }
        }

        private IEnumerator SimulateLineDismissal(Action onDismissalComplete)
        {
            yield return new WaitForSeconds(simulatedLineDismissalTime);
            onDismissalComplete();
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            switch (dialogueLine.Status)
            {
                case LineStatus.Interrupted:
                    isLineInterrupted = true;
                    break;
                default:
                    // no-op; we don't care about other states in this mock
                    // view
                    break;
            }
        }

        // A Yarn command that receives integer parameters
        [YarnCommand("testCommandInteger")]
        public void TestCommandIntegers(int a, int b) {
            Debug.Log($"{a+b}");
        }

        // A Yarn command that receives string parameters
        [YarnCommand("testCommandString")]
        public void TestCommandStrings(string a, string b) {
            Debug.Log($"{a+b}");
        }

        // A Yarn command that receives a game object parameter
        [YarnCommand("testCommandGameObject")]
        public void TestCommandGameObject(GameObject go) {
            if (go != null) {
                Debug.Log($"{go.name}");
            } else {
                Debug.Log($"(null)");
            }           
        }

        // A Yarn command that receives a component parameter
        [YarnCommand("testCommandComponent")]
        public void TestCommandComponent(MeshRenderer r) {
            if (r != null) {
                Debug.Log($"{r.name}'s MeshRenderer");
            } else {
                Debug.Log($"(null)");
            }
        }

        // A Yarn command that has optional parameters
        [YarnCommand("testCommandOptionalParams")]
        public void TestCommandOptionalParams(int a, int b = 2) {
            Debug.Log($"{a + b}");
        }

        // A Yarn command that receives no parameters
        [YarnCommand("testCommandNoParameters")]
        public void TestCommandNoParameters() {
            Debug.Log($"success");
        }

        // A Yarn command that begins a coroutine
        [YarnCommand("testCommandCoroutine")]
        public IEnumerator TestCommandCoroutine(int frameDelay) {
            // Wait the specified number of frames
            while (frameDelay > 0) {
                frameDelay -= 1;
                yield return null;
            }
            Debug.Log($"success {Time.frameCount}");
        }

        [YarnCommand]
        public void testCommandDefaultName()
        {
            Debug.Log("success");
        }

        [YarnCommand("testCommandCustomInjector", Injector = nameof(GetInstance))]
        public void TestCommandCustomInjector()
        {
            Debug.Log("success");
        }
    }

    [YarnStateInjector(nameof(GetInstance))]
    public class CustomInjector
    {
        private static CustomInjector _instance;
        private static CustomInjector GetInstance(string _)
        {
            if (_instance == null) {
                _instance = new CustomInjector();
            }
            
            Debug.Log(_instance);
            return _instance;
        }

        private static MeshRenderer CustomGetComponent(string name)
        {
            Debug.Log($"Got {name}");
            return GameObject.Find(name)?.GetComponent<MeshRenderer>();
        }

        [YarnCommand("testStaticCommand")]
        public static void TestStaticCommand()
        {
            Debug.Log("success");
        }

        [YarnCommand("testPrivateStaticCommand")]
        private static void TestPrivateStaticCommand()
        {
            Debug.Log("success");
        }

        [YarnFunction("testFnVariable")]
        public static int TestFunctionVariable(int num)
        {
            return num * num;
        }

        [YarnFunction("testFnLiteral")]
        public static string TestFunctionVariable(string text)
        {
            return $"{text} no you're not! {text}";
        }

        [YarnCommand("testCustomParameter")]
        private static void TestCustomParameter([YarnParameter(nameof(CustomGetComponent))] MeshRenderer _)
        {
            // no-op
        }

        [YarnCommand("testClassWideCustomInjector")]
        public void TestClassWideCustomInjector()
        {
            Debug.Log("success");
        }

        [YarnCommand("testPrivate")]
        private void TestPrivate()
        {
            Debug.Log("success");
        }
    }

    // test that derived methods don't register
    public class CustomInjectorDerived : CustomInjector { }
}
