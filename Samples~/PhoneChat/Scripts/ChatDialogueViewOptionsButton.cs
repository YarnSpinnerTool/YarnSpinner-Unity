using System;
using UnityEngine;
using TMPro;

#nullable enable

public class ChatDialogueViewOptionsButton : MonoBehaviour
{
    private TMP_Text? TextView => GetComponentInChildren<TMP_Text>();

    public string Text
    {
        get => (TextView != null) ? TextView.text : string.Empty;
        set { if (TextView != null) { TextView.text = value; } }
    }

    public Func<bool>? OnClick { get; internal set; }
    public void OnClicked()
    {
        OnClick?.Invoke();
    }
}