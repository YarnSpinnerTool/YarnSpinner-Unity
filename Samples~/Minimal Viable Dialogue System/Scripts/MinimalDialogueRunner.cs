using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.Events;
using System.Linq;

// This class is an example of how you can make your own dialogue runners for when you need total control over the dialogue.
// This runner is about as basic as it can be and does no checking or timing and will just move through dialogue when told.
// This is designed purely as an example of how you can do this all yourself if you want/need to
// and is based on the more complete and fancy runner in Runtime/DialogueRunner.cs
// At each relevant moment (so commands, lines, options) it halts and awaits instructions.
// When a line is reached the Continue() method must be called or else the dialogue will halt.
// When a command is reached the Continue() method must be called or the dialogue will halt.
// When an option is reached the SetSelectedOption(index) method must be called, Continue() must NOT be called.
// This runner works by sending out Unity events for relevant situations, which ANY object can respond to.
// As these are events you can hook them up to their relevant responders in the editor.
public class MinimalDialogueRunner : MonoBehaviour
{
    public YarnProject project;
    public VariableStorageBehaviour VariableStorage;
    public Yarn.Unity.LineProviderBehaviour LineProvider;

    private Yarn.Dialogue dialogue;
    public bool isRunning { get; internal set; } = false;

    void Awake()
    {
        dialogue = CreateDialogueInstance();
        dialogue.SetProgram(project.GetProgram());

        if (LineProvider == null)
        {
            // we don't have a line provider, create one and use that
            LineProvider = gameObject.AddComponent<TextLineProvider>();
        }
        LineProvider.YarnProject = project;
    }

    private Yarn.Dialogue CreateDialogueInstance()
    {
        if (VariableStorage == null)
        {
            // If we don't have a variable storage, create an
            // InMemoryVariableStorage and make it use that.
            VariableStorage = gameObject.AddComponent<InMemoryVariableStorage>();
        }

        // Create the main Dialogue runner, and pass our
        // variableStorage to it
        var dialogue = new Yarn.Dialogue(VariableStorage)
        {
            // Set up the logging system.
            LogDebugMessage = delegate (string message)
            {
                Debug.Log(message);
            },
            LogErrorMessage = delegate (string message)
            {
                Debug.LogError(message);
            },

            LineHandler = HandleLine,
            CommandHandler = HandleCommand,
            OptionsHandler = HandleOptions,
            NodeStartHandler = HandleNodeStarted,
            NodeCompleteHandler = HandleNodeEnded,
            DialogueCompleteHandler = HandleDialogueComplete,
            PrepareForLinesHandler = PrepareForLines
        };
        return dialogue;
    }

    // call to start dialogue
    public void StartDialogue(string nodeName = "Start")
    {
        if (isRunning)
        {
            Debug.LogWarning("Can't start a dialogue that is already running");
            return;
        }
        isRunning = true;
        dialogue.SetNode(nodeName);
        dialogue.Continue();
    }
    // call to stop dialogue
    public void StopDialogue()
    {
        dialogue.Stop();
        isRunning = false;
    }

    public bool NodeExists(string nodeName) => dialogue.NodeExists(nodeName);

    // called when options are encountered in the dialogue
    public UnityEvent<Yarn.Unity.DialogueOption[]> OptionsNeedPresentation;
    private void HandleOptions(Yarn.OptionSet options)
    {
        DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++)
        {
            var line = LineProvider.GetLocalizedLine(options.Options[i].Line);
            var text = Yarn.Dialogue.ExpandSubstitutions(line.RawText, options.Options[i].Line.Substitutions);
            dialogue.LanguageCode = LineProvider.LocaleCode;
            line.Text = dialogue.ParseMarkup(text);

            optionSet[i] = new DialogueOption
            {
                TextID = options.Options[i].Line.ID,
                DialogueOptionID = options.Options[i].ID,
                Line = line,
                IsAvailable = options.Options[i].IsAvailable,
            };
        }
        OptionsNeedPresentation?.Invoke(optionSet);
    }

    // called when a command is encountered in the dialogue
    // wait is handled here, all other commands are not
    public UnityEvent<string[]> CommandNeedsHandling;
    private void HandleCommand(Yarn.Command command)
    {
        // yes I do see the irony in using the full dialogue runner to make a minimal one
        var elements = Yarn.Unity.DialogueRunner.SplitCommandText(command.Text).ToArray();

        // wait is special cased, we do it ourselves
        // we need to wait for the duration listed
        if (elements[0] == "wait")
        {
            if (elements.Length < 2)
            {
                Debug.LogWarning("Asked to wait but given no duration!");
                return;
            }
            float duration = float.Parse(elements[1]);
            if (duration > 0)
            {
                IEnumerator Wait(float time)
                {
                    isRunning = false;
                    yield return new WaitForSeconds(time);
                    isRunning = true;
                    Continue();
                }
                StartCoroutine(Wait(duration));
            }
        }
        else
        {
            CommandNeedsHandling?.Invoke(elements);
        }
    }

    // called when a line is reached in the dialogue
    public UnityEvent<Yarn.Unity.LocalizedLine> LineNeedsPresentation;
    private void HandleLine(Yarn.Line line)
    {
        var finalLine = LineProvider.GetLocalizedLine(line);
        var text = Yarn.Dialogue.ExpandSubstitutions(finalLine.RawText, line.Substitutions);
        dialogue.LanguageCode = LineProvider.LocaleCode;
        finalLine.Text = dialogue.ParseMarkup(text);
        
        LineNeedsPresentation?.Invoke(finalLine);
    }

    // called when a node is entered
    public UnityEvent<string> NodeStarted;
    private void HandleNodeStarted(string nodeName)
    {
        NodeStarted?.Invoke(nodeName);
    }
    // called when a node is finished
    public UnityEvent<string> NodeEnded;
    private void HandleNodeEnded(string nodeName)
    {
        NodeEnded?.Invoke(nodeName);
    }
    // called when all dialogue is finished
    public UnityEvent DialogueComplete;
    private void HandleDialogueComplete()
    {
        isRunning = false;
        DialogueComplete?.Invoke();
    }

    private void PrepareForLines(IEnumerable<string> lineIDs)
    {
        LineProvider.PrepareForLines(lineIDs);
    }

    // call this method to advance the dialogue
    // if you spam this it will blitz through and not care
    // you have the power, use it wisely.
    public void Continue()
    {
        if (!isRunning)
        {
            Debug.LogWarning("Can't continue dialogue when we aren't currently running any");
            return;
        }

        dialogue.Continue();
    }
    // call this method when you need to choice which option is selected
    // do not call continue afterwards, this method handles that itself
    public void SetSelectedOption(int optionIndex)
    {
        if (!isRunning)
        {
            Debug.LogWarning("Can't select an option when not currently running dialogue");
            return;
        }
        dialogue.SetSelectedOption(optionIndex);
        dialogue.Continue();
    }
}
