/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using Yarn.Unity.Legacy;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0612 // Type or member is obsolete

    public class LineViewTests : IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueViewTestSceneGUID = "bc2ecf8bfda3a4a819ef357f6f85cfb6";

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueViewTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueViewTestSceneGUID);
        }

        [AllowNull]
        DialogueRunner dialogueRunner;
        [AllowNull]
        LineView lineView;
        [AllowNull]
        OptionsListView optionsView;

        [UnitySetUp]
        public IEnumerator LoadScene() => YarnTask.ToCoroutine(async () =>
        {
            SceneManager.LoadScene("DialogueViewTests");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };

            while (!loaded)
            {
                await YarnTask.Yield();
            }

            dialogueRunner = UnityEngine.Object.FindAnyObjectByType<DialogueRunner>();
            dialogueRunner.Should().NotBeNull();

            lineView = dialogueRunner.GetComponentInChildren<LineView>();
            optionsView = dialogueRunner.GetComponentInChildren<OptionsListView>();

            lineView.Should().NotBeNull();
            optionsView.Should().NotBeNull();

            dialogueRunner.DialoguePresenters.Should().Contain(lineView);
            dialogueRunner.DialoguePresenters.Should().Contain(optionsView);
            dialogueRunner.YarnProject!.Should().NotBeNull();

            // Tests may need to control which node runs, so automatically
            // starting a fixed node is not a great idea. Ensure that we're not
            // autostarting here, as a quick way to check against accidental
            // changes in the fixture's scene file.
            dialogueRunner.autoStart.Should().BeFalse();
        });


        [UnityTest]
        public IEnumerator LineView_WhenLineRun_ShowsLine() => YarnTask.ToCoroutine(async () =>
        {

            LocalizedLine line = MakeLocalizedLine(
                "Mae: Well, [b]this[/b] is {0}.",
                new[] { "great" },
                new[] { "#metadata" }
            );

            lineView.canvasGroup!.alpha.Should().BeEqualTo(0, "The line view is not yet visible");

            var runTask = lineView.RunLineAsync(line, default);

            await YarnTask.Delay(TimeSpan.FromSeconds(0.5f));

            runTask.IsCompleted().Should().BeFalse("we're still running the line");

            lineView.lineText!.text.Should().BeEqualTo("Well, this is great.");
            lineView.characterNameText!.text.Should().BeEqualTo("Mae");

            lineView.canvasGroup.alpha.Should().BeEqualTo(1, "the line is now visible");
        });

        private LocalizedLine MakeLocalizedLine(string lineText, string[]? substitutions = null, string[]? metadata = null, string? lineID = null)
        {
            string locale = "en-AU";
            Markup.MarkupParseResult ParseMarkup(string text, string[] substitutions)
            {
                var expandedText = Markup.LineParser.ExpandSubstitutions(text, substitutions);
                var lineParser = new Markup.LineParser();
                var builtinReplacer = new Markup.BuiltInMarkupReplacer();
                lineParser.RegisterMarkerProcessor("select", builtinReplacer);
                lineParser.RegisterMarkerProcessor("plural", builtinReplacer);
                lineParser.RegisterMarkerProcessor("ordinal", builtinReplacer);

                return lineParser.ParseString(expandedText, locale);
            }

            return new LocalizedLine()
            {
                TextID = lineID ?? $"line:{UnityEngine.Random.Range(0, 1000):D4}",
                RawText = lineText,
                Substitutions = substitutions ?? new string[] { },
                Metadata = metadata ?? System.Array.Empty<string>(),
                Text = ParseMarkup(lineText, substitutions ?? new string[] { }),
            };
        }

        [UnityTest]
        // [Timeout(200)] // should complete basically immediately
        public IEnumerator LineView_WhenManuallyAdvancingLine_CompletesLineTask() => YarnTask.ToCoroutine(async () =>
        {
            LocalizedLine line = MakeLocalizedLine("Line 1");

            // Configure the line view to display the entire line immediately
            lineView.useFadeEffect = false;
            lineView.useTypewriterEffect = false;

            var cancellationSource = new CancellationTokenSource();

            // Set the line view's 'interrupt handler' to be one that soft-cancels the line
            lineView.requestInterrupt = () => cancellationSource.Cancel();

            var lineCancellationToken = new LineCancellationToken
            {
                NextLineToken = cancellationSource.Token
            };

            YarnTask runTask = lineView.RunLineAsync(line, lineCancellationToken);

            runTask.IsCompleted().Should().BeFalse();
            lineView.lineText!.text.Should().BeEqualTo("Line 1");

            lineView.UserRequestedViewAdvancement();

            await runTask;

            lineView.canvasGroup!.alpha.Should().BeEqualTo(0, "The line view should now be dismissed");
        });

        [UnityTest]
        public IEnumerator LineView_TextEffects_RenderTextGradually() => YarnTask.ToCoroutine(async () =>
        {
            LocalizedLine line = MakeLocalizedLine("Line 1");

            // Configure the line view to use all of the effects
            lineView.useFadeEffect = true;
            lineView.useTypewriterEffect = true;
            lineView.fadeOutTime = 1.0f;

            var cancellationSource = new CancellationTokenSource();

            // Set the line view's 'interrupt handler' to be one that soft-cancels the line
            lineView.requestInterrupt = () => cancellationSource.Cancel();

            var lineCancellationToken = new LineCancellationToken
            {
                NextLineToken = cancellationSource.Token
            };

            YarnTask runTask = lineView.RunLineAsync(line, lineCancellationToken);

            int characterCount = lineView.lineText!.textInfo.characterCount;
            characterCount.Should().BeGreaterThan(0);
            lineView.lineText.maxVisibleCharacters.Should().BeEqualTo(0, "The typewriter effect has not yet begun");

            await YarnTask.Delay(TimeSpan.FromSeconds(0.05f));

            lineView.canvasGroup!.alpha.Should().BeGreaterThan(0);
            lineView.canvasGroup.alpha.Should().BeLessThan(1);
            lineView.lineText.maxVisibleCharacters.Should().BeEqualTo(0, "The typewriter effect has not yet begun");

            // Wait for the fade to finish
            await YarnTask.Delay(TimeSpan.FromSeconds(lineView.fadeInTime));

            lineView.canvasGroup.alpha.Should().BeEqualTo(1);

            lineView.lineText.maxVisibleCharacters.Should().BeGreaterThanOrEqualTo(0, "the typewriter effect has begun");
            lineView.lineText.maxVisibleCharacters.Should().BeLessThan(characterCount, "the entire line should not yet be visible");

            // Wait for the typewriter effect to complete
            await YarnTask.Delay(TimeSpan.FromSeconds(2f));

            lineView.lineText.maxVisibleCharacters.Should().BeGreaterThanOrEqualTo(characterCount);
            lineView.continueButton!.activeInHierarchy.Should().BeTrue();

            // Dismiss the line
            lineView.UserRequestedViewAdvancement();

            runTask.IsCompleted().Should().BeFalse();

            // Wait for the fade out to complete
            await YarnTask.Delay(TimeSpan.FromSeconds(lineView.fadeOutTime));
            await YarnTask.Delay(TimeSpan.FromSeconds(0.05));

            runTask.IsCompleted().Should().BeTrue();
            lineView.canvasGroup.alpha.Should().BeEqualTo(0);
        });
    }
}
