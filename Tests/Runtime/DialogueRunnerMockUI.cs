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

        private void Awake()
        {
            instance = this;
        }

        // runs the line complete callback
        // without this
        public void Advance()
        {
            lineDelivered();
        }

        private Action lineDelivered;
        public override void RunLine(LocalizedLine dialogueLine, Action onLineDeliveryComplete)
        {
            // Store the localised text in our CurrentLine property and
            // capture the completion handler so it can be called at
            // the correct moment later by the test system
            CurrentLine = dialogueLine.Text.Text;
            lineDelivered = onLineDeliveryComplete;
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
            onDismissalComplete();
        }

        public override void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            onDialogueLineFinished();
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

        [YarnCommand("testStaticCommand")]
        public static void TestStaticCommand()
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
    }

}
