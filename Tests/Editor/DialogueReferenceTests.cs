using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

namespace Yarn.Unity.Tests
{
    public class DialogueReferenceTests
    {
        public const string TestYarnProjectGUID = "a9b357f08075f43cbad81d8ec757e1f7";

        public YarnProject YarnProject {
            get {
                string assetPath = AssetDatabase.GUIDToAssetPath(TestYarnProjectGUID);
                Assert.NotNull(assetPath);
                YarnProject yarnProject = AssetDatabase.LoadAssetAtPath<YarnProject>(assetPath);
                Assert.NotNull(yarnProject);
                return yarnProject;
            }
        }

        [Test]
        public void DialogueReference_CanFindValidNode() {
            var dialogueReference = new DialogueReference();
            dialogueReference.nodeName = "Start";
            dialogueReference.project = YarnProject;

            Assert.True(dialogueReference.IsValid);
        }

        [Test]
        public void DialogueReference_CannotFindInvalidNode() {
            var dialogueReference = new DialogueReference();

            // Not valid if node name doesn't exist in project
            dialogueReference.nodeName = "DoesNotExist";
            dialogueReference.project = YarnProject;

            Assert.False(dialogueReference.IsValid);
        }

        [Test]
        public void DialogueReference_IsNotValidWhenEmpty() {
            var dialogueReference = new DialogueReference();

            // Not valid if project is not set
            dialogueReference.nodeName = "Start";
            dialogueReference.project = null;
            
            Assert.False(dialogueReference.IsValid);
        }

    }
}
