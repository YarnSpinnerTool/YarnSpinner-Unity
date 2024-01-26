/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnLineTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.LocalizedLine>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine?>;
#endif

using UnityEngine;
using System.Threading;

namespace Yarn.Unity.Tests
{
    public class DialogueRunnerMockUI : Yarn.Unity.AsyncDialogueViewBase
    {
        private static DialogueRunnerMockUI instance;
        private bool readyToAdvance;

        public static DialogueRunnerMockUI? GetInstance(string name)
        {
            return name == "custom" ? instance : null;
        }

        // The text of the most recently received line that we've been
        // given
        private string? CurrentLine { get; set; } = default;

        // The text of the most recently received options that we've ben
        // given
        private List<string> CurrentOptions { get; set; } = new List<string>();

        private void Awake()
        {
            instance = this;
        }

        private Action<int>? PerformSelectOption = null;

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

        public override async YarnTask RunLineAsync(LocalizedLine line, CancellationToken token)
        {
            // Store the localised text in our CurrentLine property
            CurrentLine = line.Text.Text;

            while (!readyToAdvance) {
                await YarnTask.Yield();
            }
            readyToAdvance = false;
        }

        public override async YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            CurrentOptions.Clear();
            foreach (var option in dialogueOptions)
            {
                CurrentOptions.Add(option.Line.Text.Text);
            }

            int selectedOption = -1;
            PerformSelectOption = (option) =>
            {
                selectedOption = option;
            };
            while (selectedOption == -1) {
                await YarnTask.Yield();
            }
            return dialogueOptions[selectedOption];

        }

        public void ExpectLine(string text)
        {
            CurrentLine.Should().BeEqualTo(text);
            readyToAdvance = true;
        }

        public void SelectOption(int index) {
            PerformSelectOption.Should().NotBeNull();
            PerformSelectOption!(index);
        }

        internal void ExpectOptions(params string[] options)
        {
            CurrentOptions.Should().HaveCount(options.Length);
            for (int i = 0; i < options.Length; i++) {
                CurrentOptions[i].Should().BeEqualTo(options[i]);
            }
        }
    }

}
