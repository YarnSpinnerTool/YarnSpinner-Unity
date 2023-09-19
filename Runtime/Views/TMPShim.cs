#if !USE_TMP
using UnityEngine;

[ExecuteInEditMode]
public class TMPShim : MonoBehaviour
{
    public Color color;
    public string text;
    public int maxVisibleCharacters;
    public TextInfo textInfo;
    void OnEnable()
    {
#if UNITY_2023_2_OR_NEWER
        Debug.LogWarning("Yarn Spinner requires requires uGUI 2.0.0 or above (com.unity.ugui) be installed in the Package Manager.");
#else
        Debug.LogWarning("Yarn Spinner requires TextMeshPro (com.unity.textmeshpro) be installed in the Package Manager.");
#endif
    }

    public struct TextInfo
    {
        public int characterCount;
    }
}
#endif
