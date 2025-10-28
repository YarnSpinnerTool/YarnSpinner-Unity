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

#nullable enable

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
        LinePresenter linePresenter;
        [AllowNull]
        OptionsPresenter optionsView;

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

            linePresenter = dialogueRunner.GetComponentInChildren<LinePresenter>();
            optionsView = dialogueRunner.GetComponentInChildren<OptionsPresenter>();

            linePresenter.Should().NotBeNull();
            optionsView.Should().NotBeNull();

            dialogueRunner.DialoguePresenters.Should().Contain(linePresenter);
            dialogueRunner.DialoguePresenters.Should().Contain(optionsView);

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

            linePresenter.canvasGroup!.alpha.Should().BeEqualTo(0, "The line view is not yet visible");

            var runTask = linePresenter.RunLineAsync(line, default);

            await YarnTask.Delay(TimeSpan.FromSeconds(0.5f));

            runTask.IsCompleted().Should().BeFalse("we're still running the line");

            linePresenter.lineText!.text.Should().BeEqualTo("Well, this is great.");
            linePresenter.characterNameText!.text.Should().BeEqualTo("Mae");

            linePresenter.canvasGroup.alpha.Should().BeEqualTo(1, "the line is now visible");
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
            linePresenter.useFadeEffect = false;
            linePresenter.typewriterStyle = LinePresenter.TypewriterType.Instant;

            var cancellationSource = new CancellationTokenSource();
            var lineCancellationToken = new LineCancellationToken
            {
                NextContentToken = cancellationSource.Token
            };

            YarnTask runTask = linePresenter.RunLineAsync(line, lineCancellationToken);

            runTask.IsCompleted().Should().BeFalse();
            linePresenter.lineText!.text.Should().BeEqualTo("Line 1");

            cancellationSource.Cancel();

            await runTask;

            linePresenter.canvasGroup!.alpha.Should().BeEqualTo(0, "The line view should now be dismissed");
        });

        [UnityTest]
        public IEnumerator LineView_TextEffects_RenderTextGradually() => YarnTask.ToCoroutine(async () =>
        {
            LocalizedLine line = MakeLocalizedLine("Line 1");

            // Configure the line view to use all of the effects
            linePresenter.useFadeEffect = true;
            linePresenter.typewriterStyle = LinePresenter.TypewriterType.ByLetter;
            linePresenter.fadeDownDuration = 1.0f;
            linePresenter.fadeUpDuration = 1f;

            var cancellationSource = new CancellationTokenSource();
            var lineCancellationToken = new LineCancellationToken
            {
                NextContentToken = cancellationSource.Token
            };

            linePresenter.lineText.Should().NotBeNull();

            var continueButton = linePresenter.GetComponentInChildren<LinePresenterButtonHandler>(true);
            continueButton.Should().NotBeNull();

            YarnTask runTask = linePresenter.RunLineAsync(line, lineCancellationToken);

            int characterCount = line.Text.Text.Length;
            characterCount.Should().BeGreaterThan(0);
            linePresenter.lineText!.maxVisibleCharacters.Should().BeEqualTo(0, "The typewriter effect has not yet begun");

            linePresenter.canvasGroup!.alpha.Should().BeGreaterThan(0);
            linePresenter.canvasGroup.alpha.Should().BeLessThan(1);
            linePresenter.lineText.maxVisibleCharacters.Should().BeEqualTo(0, "The typewriter effect has not yet begun");

            // Wait for the fade to finish
            await YarnTask.Delay(TimeSpan.FromSeconds(linePresenter.fadeUpDuration));

            linePresenter.canvasGroup.alpha.Should().BeEqualTo(1);

            linePresenter.lineText.maxVisibleCharacters.Should().BeGreaterThanOrEqualTo(0, "the typewriter effect has begun");
            linePresenter.lineText.maxVisibleCharacters.Should().BeLessThan(characterCount, "the entire line should not yet be visible");

            // Wait for the typewriter effect to complete
            await YarnTask.Delay(TimeSpan.FromSeconds(2f));

            linePresenter.lineText.maxVisibleCharacters.Should().BeEqualTo(characterCount);
            continueButton.gameObject.activeInHierarchy.Should().BeTrue();

            // Dismiss the line
            cancellationSource.Cancel();

            runTask.IsCompleted().Should().BeFalse();

            // Wait for the fade out to complete
            await YarnTask.Delay(TimeSpan.FromSeconds(linePresenter.fadeDownDuration));
            await YarnTask.Delay(TimeSpan.FromSeconds(0.05));

            runTask.IsCompleted().Should().BeTrue();
            linePresenter.canvasGroup.alpha.Should().BeEqualTo(0);
        });
    }
}
