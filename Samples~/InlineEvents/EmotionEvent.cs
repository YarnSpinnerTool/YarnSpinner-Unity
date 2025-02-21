/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity.Samples
{
    public class EmotionEvent : ActionMarkupHandler
    {
        private SimpleCharacter target;
        Dictionary<int, string> emotions;

        public override void OnLineDisplayComplete()
        {
            return;
        }

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            return;
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
        {
            // grab the character attribute
            // grab the SimpleCharacterAnimation that matches that name
            if (!line.TryGetAttributeWithName("character", out var character))
            {
                Debug.LogWarning("line has no character");
                return;
            }
            // we need the name of the character so we can find them in the scene
            if (!character.Properties.TryGetValue("name", out var name))
            {
                Debug.LogWarning("character has no name");
                return;
            }

            var emoter = GameObject.Find(name.StringValue);
            if (emoter == null)
            {
                Debug.LogWarning($"scene has no one called {name.StringValue}");
                return;
            }

            target = emoter.GetComponent<SimpleCharacter>();
            if (target == null)
            {
                Debug.Log($"{name.StringValue} is not a SimpleCharacterAnimation");
                return;
            }

            emotions = new();

            foreach (var attribute in line.Attributes)
            {
                if (attribute.Name != "emotion")
                {
                    continue;
                }

                if (!attribute.TryGetProperty("emotion", out string emotionKey))
                {
                    continue;
                }
                emotions[attribute.Position] = emotionKey;
            }
        }

        public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (target == null)
            {
                return YarnTask.CompletedTask;
            }

            if (emotions.TryGetValue(currentCharacterIndex, out var emotion))
            {
                target.SetFacialExpression(emotion);
            }

            return YarnTask.CompletedTask;
        }

        public override void OnLineWillDismiss()
        {
            return;
        }
    }
}