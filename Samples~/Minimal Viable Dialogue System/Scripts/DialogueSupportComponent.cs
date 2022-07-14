using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class exists to provide some support structure for the minimal dialogue system, it does two things:
// 1. gives a means to start the dialogue
// 2. "handles" commands in the dialogue
// It does these in a very basic manner, just calling start on the runner and logging the commands
// and is not designed to be a full solution for either of these, just something to make the sample work.
public class DialogueSupportComponent : MonoBehaviour
{
    MinimalDialogueRunner runner;
    void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!runner.isRunning)
            {
                runner.StartDialogue();
            }
        }
    }

    public void HandleCommand(string[] commandText)
    {
        Debug.Log($"Received a command: {commandText[0]}");
        runner.Continue();
    }
    public void LogNodeStarted(string node)
    {
        Debug.Log($"entered node {node}");
    }
    public void LogNodeEnded(string node)
    {
        Debug.Log($"exited node {node}");
    }
    public void LogDialogueEnded()
    {
        Debug.Log("Dialogue has finished");
    }
}
