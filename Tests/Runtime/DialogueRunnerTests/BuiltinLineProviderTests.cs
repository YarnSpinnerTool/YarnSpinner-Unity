/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using UnityEngine.UI;
    using Yarn.Markup;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// An IMarkupParser that performs no processing.
    /// </summary>
    internal class FakeMarkupParser : IMarkupParser
    {
        public MarkupParseResult ParseMarkup(string rawText, string localeCode)
        {
            return new MarkupParseResult(rawText, new());
        }
    }

    [TestFixture]
    public class BuiltInLineProviderTests : IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueRunnerTestSceneGUID = "a04d7174042154a47a29ac4f924e0474";
        const string TestResourcesFolderGUID = "be395506411a5a74eb2458a5cf1de710";

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueRunnerTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueRunnerTestSceneGUID);
        }

        [AllowNull]
        private DialogueRunner runner;
        [AllowNull]
        private BuiltinLocalisedLineProvider lineProvider;
        [AllowNull]
        private YarnProject yarnProject;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };

            yield return new WaitUntil(() => loaded);

            yield return null; // Give all objects a chance to wake up

            runner = GameObject.FindAnyObjectByType<DialogueRunner>();
            runner.Should().NotBeNull();
            runner.YarnProject.Should().NotBeNull();
            runner.LineProvider.Should().NotBeNull();

            lineProvider = runner.LineProvider.Should().BeOfExactType<BuiltinLocalisedLineProvider>().Subject;

            lineProvider.Should().NotBeNull();
            runner.LineProvider.Should().BeSameObjectAs(lineProvider);

            lineProvider.YarnProject.Should().NotBeNull();

            yarnProject = runner.yarnProject!;
            yarnProject.Should().NotBeNull();

            lineProvider.YarnProject.Should().BeSameObjectAs(yarnProject);
        }

        [UnityTest]
        public IEnumerator LineProvider_CorrectLineID_FetchesLineContent() => YarnTask.ToCoroutine(async () =>
        {
            var line = new Line("line:shadowtest_1", new string[] { });

            lineProvider.LocaleCode = "en";
            var localisedLine = await lineProvider.GetLocalizedLineAsync(line, CancellationToken.None);

            localisedLine.Should().NotBeNull();

            localisedLine.TextID.Should().BeEqualTo("line:shadowtest_1");
            localisedLine.Asset!.Should().NotBeNull();
            localisedLine.Asset!.Should().BeOfType<AudioClip>();
            localisedLine.CharacterName!.Should().NotBeNull();
            localisedLine.CharacterName!.Should().BeEqualTo("Ava");
        });

        [UnityTest]
        public IEnumerator LineProvider_IncorrectLineID_FetchesInvalidLineMarker() => YarnTask.ToCoroutine(async () =>
        {
            var line = new Line("line:doesnotexist", new string[] { });

            lineProvider.LocaleCode = "en";
            var localisedLine = await lineProvider.GetLocalizedLineAsync(line, CancellationToken.None);

            localisedLine.Should().BeSameObjectAs(LocalizedLine.InvalidLine);
        });

        [UnityTest]
        public IEnumerator LineProvider_ShadowLineID_FetchesSourceContent() => YarnTask.ToCoroutine(async () =>
        {

            lineProvider.LocaleCode = "en";

            // Find the shadow line in 'ShadowLines_Kitchen' - it'll be the only
            // line whose ID doesn't start with "shadowtest"
            var allLines = yarnProject.GetLineIDsForNodes(new[] { "ShadowLines_Kitchen" });
            var shadowLineID = allLines.Should().ContainSingle(l => l.StartsWith("line:shadowtest_") == false).Subject;

            var sourceLineID = yarnProject.lineMetadata?.GetShadowLineSource(shadowLineID)!;
            sourceLineID.Should().NotBeNull();

            var sourceLine = await lineProvider.GetLocalizedLineAsync(new Line(sourceLineID, new string[] { }), CancellationToken.None);
            var shadowLine = await lineProvider.GetLocalizedLineAsync(new Line(shadowLineID, new string[] { }), CancellationToken.None);

            shadowLine.TextID.Should().NotBeEqualTo(sourceLineID, "shadow lines have their own unique line IDs");
            shadowLine.RawText!.Should().BeEqualTo(sourceLine.RawText, "shadow lines have the same text as their source");
            shadowLine.Asset!.Should().BeSameObjectAs(sourceLine.Asset!, "shadow lines should have the same asset as their source");

            shadowLine.Metadata.Should().NotBeEmpty("shadow line contains metadata");
            shadowLine.Metadata.Should().NotContainAnyOf(sourceLine.Metadata, "shadow line does not contain its source line's metadata");
        });

    }
}
