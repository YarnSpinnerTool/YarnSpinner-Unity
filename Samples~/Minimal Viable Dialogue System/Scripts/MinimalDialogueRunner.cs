using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.Events;
using System.Linq;

public class MinimalDialogueRunner : MonoBehaviour
{
    public YarnProject project;
    public VariableStorageBehaviour VariableStorage;
    public Yarn.Unity.LineProviderBehaviour LineProvider;

    private Yarn.Dialogue dialogue;
    private bool isRunning = false;

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
    public void StopDialogue()
    {
        dialogue.Stop();
        isRunning = false;
    }

    public bool NodeExists(string nodeName) => dialogue.NodeExists(nodeName);

    public OptionArrayEvent OptionsNeedPresentation;
    private void HandleOptions(Yarn.OptionSet options)
    {
        DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++)
        {
            var line = LineProvider.GetLocalizedLine(options.Options[i].Line);
            var text = Yarn.Dialogue.ExpandSubstitutions(line.RawText, options.Options[i].Line.Substitutions);
            dialogue.LanguageCode = LineProvider.textLanguageCode;
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

    public CommandUnityEvent CommandNeedsHandling;
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

    public LineUnityEvent LineNeedsPresentation;
    private void HandleLine(Yarn.Line line)
    {
        var finalLine = LineProvider.GetLocalizedLine(line);
        var text = Yarn.Dialogue.ExpandSubstitutions(finalLine.RawText, line.Substitutions);
        dialogue.LanguageCode = LineProvider.textLanguageCode;
        finalLine.Text = dialogue.ParseMarkup(text);
        
        LineNeedsPresentation?.Invoke(finalLine);
    }

    public StringUnityEvent NodeStarted;
    private void HandleNodeStarted(string nodeName)
    {
        NodeStarted?.Invoke(nodeName);
    }
    public StringUnityEvent NodeEnded;
    private void HandleNodeEnded(string nodeName)
    {
        NodeEnded?.Invoke(nodeName);
    }
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

    public void Continue()
    {
        if (!isRunning)
        {
            Debug.LogWarning("Can't continue dialogue when we aren't currently running any");
            return;
        }

        dialogue.Continue();
    }
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

[System.Serializable] public class StringUnityEvent : UnityEvent<string> {}
[System.Serializable] public class LineUnityEvent : UnityEvent<Yarn.Unity.LocalizedLine> {}
[System.Serializable] public class LineArrayUnityEvent : UnityEvent<Yarn.Unity.LocalizedLine[]> {}
[System.Serializable] public class CommandUnityEvent : UnityEvent<string[]> {}
[System.Serializable] public class OptionArrayEvent : UnityEvent<Yarn.Unity.DialogueOption[]> {}
