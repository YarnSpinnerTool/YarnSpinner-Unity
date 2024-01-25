/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitGameCommand : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<Yarn.Unity.DialogueRunner>().AddCommandHandler("quit", () => Quit());
    }

    private void Quit()
    {
        // In the editor, leave Play Mode. In a regular build, quit the
        // app.
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif        
    }
}
