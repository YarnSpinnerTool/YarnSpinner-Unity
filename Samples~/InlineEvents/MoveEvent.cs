/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Markup;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity.Samples
{
    public class MoveEvent : ActionMarkupHandler
    {
        private Dictionary<int, Vector3> movements = new();

        // basically when we hit the specific point I will make the dude walk
        public override void OnLineDisplayComplete()
        {
            movements.Clear();
        }

        public SimpleCharacter playerCharacter;

        void Start()
        {
            var runner = GameObject.FindAnyObjectByType<Yarn.Unity.DialogueRunner>();
            runner.AddCommandHandler<string>("move", MoveCharacter);
        }

        public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            return;
        }

        public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
        {
            // now we want all of the move markup
            foreach (var attribute in line.Attributes)
            {
                if (attribute.Name == "move")
                {
                    if (attribute.TryGetProperty("name", out string namedPos))
                    {
                        var position = GameObject.Find(namedPos);
                        if (position != null)
                        {
                            movements[attribute.Position] = position.transform.position;
                        }
                    }
                }
            }
        }

        public override async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (movements.TryGetValue(currentCharacterIndex, out var position))
            {
                await playerCharacter.MoveTo(position, cancellationToken);
            }
        }

        public async YarnTask MoveCharacter(string endMarker)
        {
            var position = GameObject.Find(endMarker);
            if (position == null)
            {
                return;
            }
            await playerCharacter.MoveTo(position.transform.position, CancellationToken.None);
        }

        public override void OnLineWillDismiss()
        {
            return;
        }
    }
}