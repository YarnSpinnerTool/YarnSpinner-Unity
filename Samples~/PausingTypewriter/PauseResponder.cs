using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseResponder : MonoBehaviour
{
    [SerializeField] Image face;
    [SerializeField] Sprite thinkingFace;
    [SerializeField] Sprite talkingFace;

    public void OnPauseStarted()
    {
        face.sprite = thinkingFace;
    }
    public void OnPauseEnded()
    {
        face.sprite = talkingFace;
    }
}
