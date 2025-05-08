/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity
{
    /*
    This class exists for the purpose of resolving a VERY unlikely to occur package dependancy issue.
    Previously we used to have a dependancy on Text Mesh Pro (and still do), but in Unity 2023 unity deprecated TMP and merged it into uGUI 2.0.0 but did it in a very silly way that caused a package resolution issue if you have it installed in 2023.
    Because you can't easily set Unity version dependant packaging, the dependancy on either was removed and we rely on, that by default, one of either TMP or uGUI is installed.

    Essentially we moved from a hard dependancy on TMP to a soft one.
    This means it's possible that neither is installed (very unlikely) and we need to handle it.

    That is what this class is for, it emulates the basic shape of TMP as far as dialogue views are concerned and can stand in for the proper TMP elements.
    The reason we did it this way is so that any serialised elements of those dialogue views aren't lost when the user sees the error and then installs TMP.
    */
#if !USE_TMP
    using UnityEngine;

    [ExecuteInEditMode]
    public sealed class TMPShim : MonoBehaviour
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
}
