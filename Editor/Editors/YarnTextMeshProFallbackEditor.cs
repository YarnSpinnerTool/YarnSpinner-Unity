/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#if !USE_TMP
using UnityEditor;

namespace Yarn.Unity.Editor
{
	/// <summary>
	/// When TextMeshPro (com.unity.textmeshpro) or UGUI 2.0.0+ (com.unity.ugui) are not present in the project, TMP is not supported.<br/>
	/// This editor will draw a help box for the classes that require TMP to function.
	/// </summary>
	public abstract class YarnTextMeshProFallbackEditor : UnityEditor.Editor
	{
        public override void OnInspectorGUI()
        {
#if UNITY_2023_2_OR_NEWER
            EditorGUILayout.HelpBox("This component requires uGUI 2.0.0 or above (com.unity.ugui) be installed in the Package Manager.", MessageType.Warning);
#else
			EditorGUILayout.HelpBox("This component requires TextMeshPro (com.unity.textmeshpro) be installed in the Package Manager.", MessageType.Warning);
#endif
        }
	}

	[CustomEditor(typeof(LineView))]
	public sealed class LineViewEditorFallback : YarnTextMeshProFallbackEditor { }
	
	[CustomEditor(typeof(OptionsListView))]
	public sealed class OptionsListViewEditorFallback : YarnTextMeshProFallbackEditor { }

	[CustomEditor(typeof(CharacterColorView))]
	public sealed class CharacterColorViewEditorFallback : YarnTextMeshProFallbackEditor { }

	[CustomEditor(typeof(OptionView))]
	public sealed class OptionViewEditorFallback : YarnTextMeshProFallbackEditor { }
}
#endif
