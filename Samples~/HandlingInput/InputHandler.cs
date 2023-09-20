using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class InputHandler : MonoBehaviour
{
    public float fadeDuration = 1f;
    public CanvasGroup inputGroup;
    private DialogueRunner runner;
    private string input;
    
    void Start()
    {
        runner = FindObjectOfType<DialogueRunner>();
        runner.AddCommandHandler<string>("input", InputRenamer);
    }

    /// <summary>
    /// A blocking command that summons an input field, waits for input, and then stores the input into the variable.
    /// </summary>
    /// <param name="variable">The name of the yarn variable, without the $, to be inputted</param>
    public IEnumerator InputRenamer(string variable)
    {
        var accumulator = 0f;
        while (accumulator < fadeDuration)
        {
            var alpha = Mathf.Lerp(0, 1, accumulator/fadeDuration);
            inputGroup.alpha = alpha;
            accumulator += Time.deltaTime;
            yield return null;
        }
        inputGroup.alpha = 1;

        while (string.IsNullOrWhiteSpace(input))
        {
            yield return null;
        }

        runner.VariableStorage.SetValue($"${variable}", input);
        input = null;

        accumulator = 0;
        while (accumulator < fadeDuration)
        {
            var alpha = Mathf.Lerp(1, 0, accumulator/fadeDuration);
            inputGroup.alpha = alpha;
            accumulator += Time.deltaTime;
            yield return null;
        }
        inputGroup.alpha = 0;
    }

    // Called by the TMP input filed On Edit End event
    // This is connected in the editor
    public void OnEditEnd(string edit)
    {
        input = edit;
    }
}
