/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Saliency;
using Yarn.Unity;

/// <summary>
/// Provides a weighted random saliency selection strategy.
/// Allowing for the writers to decide how likely each option is to be selected.
/// </summary>
/// <remarks>
/// <para>
/// This strategy works by rolling a large dice where the number of sides of the dice is equal to the combined weight of all the options.
/// Each options given weight provides the range of values that that option occupy on the dice.
/// The dice roll is compared to the ranges of each option and the range the dice value falls between is the selected option.
/// </para>
/// <para>
/// Nodes are given their weight by adding a <b>weight</b> header.
/// For example <b>weight: 3</b> as a header will give that node three times as many chances to be selected if it had no weight set.
/// Line groups are given their weight by adding a <b>weight</b> metadata.
/// For example <b>#weight: 3</b> will give that line group option three times as many chances to be selected if it had no weighting.
/// If no weight is set then the salient content is given an implicit weight of 1.
/// Weights cannot be zero or negative, if they are set to this then it is assumed they are weight of 1.
/// </para>
/// <para>
/// Due to failing content being eliminated the specific chance of any piece of content being selected will not be the same through each run through.
/// For example if you have the following salient content:
/// <code>
/// => line 1 #weight:2
/// => line 2
/// => line 3 &lt;&lt;if $some_number == 5>>
/// </code>
/// Assuming that <b>$some_number</b> equals five then the chance of <b>line 1</b> being selected is 50%.
/// But if <b>$some_number</b> is not five then <b>line 1</b> chance goes up to 66%.
/// </para>
/// </remarks>
namespace Yarn.Unity.Samples
{
    public class WeightedSaliencySelector : MonoBehaviour, IContentSaliencyStrategy
    {
        public DialogueRunner runner;

        const string WeightKey = "weight";

        void Start()
        {
            // assigning ourself to be the saliency strategy
            // this is only for the demo
            if (runner == null)
            {
                runner = FindAnyObjectByType<Yarn.Unity.DialogueRunner>();
            }
            runner.Dialogue.ContentSaliencyStrategy = this;
        }
        
        // with this saliency selector we don't need to mutate any state so we can ignore this method
        public void ContentWasSelected(ContentSaliencyOption content)
        {
            return;
        }

        public ContentSaliencyOption QueryBestContent(IEnumerable<ContentSaliencyOption> content)
        {   
            // keeps the range for each option
            List<(ContentSaliencyOption option, int min, int max)> ranges = new();
            
            // determines the size of the dice
            int diceSize = 0;

            // we run through every piece of salient content and work out it's weighting
            foreach (var element in content)
            {
                // if the content has failed any of it's conditions we drop it
                if (element.FailingConditionValueCount > 0)
                {
                    continue;
                }

                string weightString = null;

                if (element.ContentType == ContentSaliencyContentType.Node)
                {
                    // if we are a node group we get the weight from the headers on the node
                    // if there is no weight header that is fine, this will be null and it will be given an implict weight of 1
                    weightString = runner.Dialogue.GetHeaderValue(element.ContentID, WeightKey);
                }
                else
                {
                    // if we are a line group we get the weight from the line metadata
                    var lineKey = WeightKey + ':';
                    foreach (var metadata in runner.YarnProject.lineMetadata.GetMetadata(element.ContentID))
                    {
                        if (metadata.StartsWith(lineKey))
                        {
                            weightString = metadata.Substring(lineKey.Length).Trim();
                            break;
                        }
                    }
                }

                // we attempt to convert the value into an int
                if (Int32.TryParse(weightString, out int weight))
                {
                    // if the weight is 0 or less we force it to be 1
                    if (weight < 1)
                    {
                        weight = 1;
                    }
                    ranges.Add((element, diceSize, diceSize + weight - 1));
                    diceSize += weight;
                }
                else
                {
                    // we have no weight explictly set so we implicitly give it a weight of 1
                    ranges.Add((element, diceSize, diceSize));
                    diceSize += 1;
                }
            }
            
            // if we have no ranges it means there is no valid content
            // so we return null to indicate nothing should be run
            if (ranges.Count == 0)
            {
                return null;
            }

            // we roll a dice now and pick the option whos range contains that roll
            int roll = UnityEngine.Random.Range(0, diceSize);
            foreach (var (option, min, max) in ranges)
            {
                if (roll <= max && roll >= min)
                {
                    return option;
                }
            }

            return null;
        }
    }
}