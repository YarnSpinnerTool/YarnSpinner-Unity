#define LOGGING

using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Represents a scripting define symbol used in the current platform's
    /// build settings.
    /// </summary>
    /// <remarks>
    /// This class provides a way to get or set whether a scripting define
    /// symbol is present in the current build settings, represented as a
    /// boolean value: <see langword="true"/> if the symbol is present, and <see
    /// langword="false"/> if not.
    /// </remarks>
    public class ScriptingDefineSymbol
    {
        /// <summary>
        /// Creates a new <see cref="ScriptingDefineSymbol"/> that represents
        /// <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the scripting define symbol.</param>
        /// <returns>A <see cref="ScriptingDefineSymbol"/> object that
        /// represents <paramref name="name"/>.</returns>
        public static ScriptingDefineSymbol GetSymbol(string name)
        {
            return new ScriptingDefineSymbol(name);
        }

        /// <summary>
        /// Gets the name of the symbol that this object represents.
        /// </summary>
        public string SymbolName { get; }

        /// <summary>
        /// Creates a new instance of ScriptingDefineSymbol that represents a
        /// symbol named <paramref name="symbolName"/>.
        /// </summary>
        /// <param name="symbolName">The name of the scripting define
        /// symbol.</param>
        private ScriptingDefineSymbol(string symbolName)
        {
            this.SymbolName = symbolName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scripting define symbol
        /// is present in the current platform's build settings.
        /// </summary>
        public bool Value
        {
            get
            {
#if UNITY_2021_2_OR_NEWER
                var currentGroup = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                return PlayerSettings
                    .GetScriptingDefineSymbols(currentGroup)
                    .Split(new[] {';'}, System.StringSplitOptions.RemoveEmptyEntries)
                    .Contains(SymbolName);
#else
                var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                return PlayerSettings
                    .GetScriptingDefineSymbolsForGroup(currentGroup)
                    .Split(new[] {';'}, System.StringSplitOptions.RemoveEmptyEntries)
                    .Contains(SymbolName);
#endif
            }

            set
            {
#if UNITY_2021_2_OR_NEWER
                var currentGroup = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                var currentDefines = PlayerSettings
                    .GetScriptingDefineSymbols(currentGroup)
                    .Split(new[] {';'}, System.StringSplitOptions.RemoveEmptyEntries);
#else
                var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var currentDefines = PlayerSettings
                    .GetScriptingDefineSymbolsForGroup(currentGroup)
                    .Split(new[] {';'}, System.StringSplitOptions.RemoveEmptyEntries);
#endif

                var currentDefinesList = new List<string>(currentDefines);

                var isPresent = currentDefines.Contains(SymbolName);

                if (value && !isPresent)
                {
                    currentDefinesList.Add(SymbolName);
                }
                else if (!value && isPresent)
                {
                    currentDefinesList.Remove(SymbolName);
                }
                else
                {
                    // Nothing to do
#if LOGGING
                    UnityEngine.Debug.Log($"SetScriptingDefineSymbolsForGroup: not {(value ? "adding" : "removing")} symbol {SymbolName} because it already {(value ? "is" : "isn't")} in the existing symbols");
#endif
                    return;
                }

                var newDefinesList = string.Join(";", currentDefinesList);

#if LOGGING
                UnityEngine.Debug.Log($"SetScriptingDefineSymbolsForGroup '{newDefinesList}'");
#endif

#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(currentGroup, newDefinesList);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(currentGroup, newDefinesList);
#endif
            }
        }
    }
}
