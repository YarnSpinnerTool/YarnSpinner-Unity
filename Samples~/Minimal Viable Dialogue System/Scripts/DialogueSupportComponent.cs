using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSupportComponent : MonoBehaviour
{
    MinimalDialogueRunner runner;
    // Start is called before the first frame update
    void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            runner.StartDialogue();
        }
    }

    public void HandleCommand(string[] commandText)
    {
        Debug.Log($"Received a command: {commandText[0]}");
        runner.Continue();
    }
}
