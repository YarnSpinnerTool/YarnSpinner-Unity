/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Yarn.Compiler.Upgrader;

namespace Yarn.Unity
{
    public class YarnSpinnerEditorWindow : EditorWindow
    {

        // Current scrolling position
        Vector2 scrollPos;

        // The list of Yarn scripts in the project. Updated by the
        // UpgradeProgram method.
        List<YarnProgram> yarnProgramList = new List<YarnProgram>();

        // The URL for the text document containing supporter information
        private const string SupportersURL = "https://yarnspinner.dev/supporters.txt";

        // URLs to open when buttons on the about page are clicked
        private const string DocumentationURL = "https://yarnspinner.dev/docs/tutorial";
        private const string PatreonURL = "https://www.patreon.com/bePatron?u=11132340";

        // The request object, used to fetch supportersText
        static UnityWebRequest supportersRequest;

        // The dynamically fetched text to show in the About page.
        static string supportersText = null;

        // Shows the window.
        [MenuItem("Window/Yarn Spinner %#y", false, 2000)]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<YarnSpinnerEditorWindow>();
        }

        // Called when the window first appears.
        void OnEnable()
        {

            // Set the window title
            this.titleContent.text = "Yarn Spinner";
            this.titleContent.image = Icons.WindowIcon;

            this.YarnSpinnerVersion = typeof(DialogueRunner).Assembly.GetName().Version.ToString();

            if (supportersText == null)
            {
                RequestSupporterText();
            }

            // Subscribe to be notified of asset changes - we'll use them
            // to refresh our asset list
            EditorApplication.projectChanged += RefreshYarnProgramList;

            // Also refresh the list right
            RefreshYarnProgramList();
        }

        private void OnDisable()
        {
            // Tidy up our update-list delegate when we're going away
            EditorApplication.projectChanged -= RefreshYarnProgramList;
        }

        private static void EditorUpdate()
        {
            if (supportersRequest == null)
            {
                // The UnityWebRequest hasn't been created, so this method
                // should be called. Remove the callback so we don't get
                // called again.
                EditorApplication.update -= EditorUpdate;
                return;
            }

            if (supportersRequest.isDone == false)
            {
                // Not done loading yet; continue waiting
                return;
            }

            EditorApplication.update -= EditorUpdate;

            if (supportersRequest.isNetworkError || supportersRequest.isHttpError)
            {
                Debug.LogError("Error loading Yarn Spinner supporter data: " + supportersRequest.error);
                supportersText = ""; // set to the empty string to prevent future loads
                return;
            }

            supportersText = supportersRequest.downloadHandler.text;

        }

        private static void RequestSupporterText()
        {
            // Start requesting the supporters text.
            supportersRequest = UnityWebRequest.Get(SupportersURL);
            supportersRequest.SendWebRequest();

            // Run EditorUpdate every editor frame so that we can handle
            // when the request ends.
            EditorApplication.update += EditorUpdate;
        }

        enum SelectedMode
        {
            About,
            UpgradeScripts,
        }

        SelectedMode selectedMode = 0;

        private string YarnSpinnerVersion;

        void OnGUI()
        {
            var modes = System.Enum.GetNames(typeof(SelectedMode))
                                   .Select((x) => ObjectNames.NicifyVariableName(x))
                                   .ToArray();

            selectedMode = (SelectedMode)GUILayout.Toolbar((int)selectedMode, modes);

            switch (selectedMode)
            {
                case SelectedMode.About:
                    DrawAboutGUI();
                    break;
                case SelectedMode.UpgradeScripts:
                    DrawUpgradeGUI();
                    break;
            }

        }

        void DrawAboutGUI()
        {

            float logoSize = Mathf.Min(EditorGUIUtility.currentViewWidth, 200);

            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
            using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins))
            {
                scrollPos = scroll.scrollPosition;

                GUIStyle titleLabel = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter
                };

                GUIStyle versionLabel = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };

                GUIStyle creditsLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };

                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(logoSize)))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(new GUIContent(Icons.Logo), GUILayout.Width(logoSize), GUILayout.Height(logoSize));
                        GUILayout.Label("Yarn Spinner", titleLabel);
                        GUILayout.Label(YarnSpinnerVersion, versionLabel);
                        GUILayout.Space(10);

                        if (GUILayout.Button("Documentation"))
                        {
                            Application.OpenURL(DocumentationURL);
                        }
                        if (GUILayout.Button("Support Us On Patreon"))
                        {
                            Application.OpenURL(PatreonURL);
                        }
                    }
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(20);
                GUILayout.Label("Yarn Spinner is made possible thanks to our wonderful supporters on Patreon.", creditsLabel);
                GUILayout.Space(20);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.currentViewWidth - 40)))
                {
                    if (supportersText == null)
                    {
                        // We're still waiting for supporters text to finish
                        // arriving (or error out)
                        GUILayout.Label("Loading supporters...", creditsLabel);
                    }
                    else
                    {
                        GUILayout.Label(supportersText, creditsLabel);
                    }
                }
            }
        }

        private void DrawUpgradeGUI()
        {
            // Show some introductory UI to explain the purpose of this
            // page
            GUILayout.Label("Upgrade your Yarn scripts from version 1 to version 2.");
            EditorGUILayout.HelpBox("Upgrading a script that's already in version 2 may throw an error, but won't modify your file.", MessageType.Info);

            // Show the list of scripts we can upgrade
            foreach (var script in yarnProgramList)
            {
                EditorGUILayout.BeginHorizontal();

                // Show the object field - disabled so people won't try and
                // replace items in the list with other scripts, which
                // won't work
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(script, typeof(YarnProgram), false);
                EditorGUI.EndDisabledGroup();

                // A little "upgrade this specific script" button
                if (GUILayout.Button("Upgrade"))
                {
                    UpgradeProgram(script, UpgradeType.Version1to2);
                }
                EditorGUILayout.EndHorizontal();
            }

            // If we have any scripts, show an Upgrade All button
            if (yarnProgramList.Count > 0)
            {
                if (GUILayout.Button("Upgrade All"))
                {
                    foreach (var script in yarnProgramList)
                    {
                        UpgradeProgram(script, UpgradeType.Version1to2);
                    }
                }
            }
        }

        private void UpgradeProgram(YarnProgram script, UpgradeType upgradeMode)
        {
            // Get the path for this asset
            var path = AssetDatabase.GetAssetPath(script);
            string fileName = Path.GetFileName(path);

            // Get the text from the path
            var originalText = File.ReadAllText(path);

            // Run it through the upgrader
            string upgradedText;
            try
            {

                upgradedText = LanguageUpgrader.UpgradeScript(
                    originalText,
                    fileName,
                    upgradeMode,
                    out var replacements);

                if (replacements.Count() == 0)
                {
                    Debug.Log($"No upgrades required for {fileName}", script);

                    // Exit here - we won't need to modify the file on
                    // disk, so we can save some effort by returning at
                    // this point
                    return;
                }

                // Log some diagnostics about what changes we're making
                foreach (var replacement in replacements)
                {
                    Debug.Log($@"{fileName}:{replacement.StartLine}: ""{replacement.OriginalText}"" -> ""{replacement.ReplacementText}""", script);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to upgrade {fileName}: {e.GetType()} {e.Message}", script);
                return;
            }

            // Save the text back to disk
            File.WriteAllText(path, upgradedText);

            // Re-import the asset
            AssetDatabase.ImportAsset(path);
        }

        private void RefreshYarnProgramList()
        {
            // Search for all Yarn scripts, and load them into the list.
            yarnProgramList = AssetDatabase.FindAssets($"t:{nameof(YarnProgram)}")
                                           .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                           .Select(path => AssetDatabase.LoadAssetAtPath<YarnProgram>(path))
                                           .ToList();

            // Repaint to ensure that any changes to the list are visible
            Repaint();
        }
    }

    // Icons used by this editor window.
    internal class Icons
    {

        private static Texture GetTexture(string textureName)
        {
            var guids = AssetDatabase.FindAssets(string.Format("{0} t:texture", textureName));
            if (guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        static Texture _successIcon;
        public static Texture SuccessIcon
        {
            get
            {
                if (_successIcon == null)
                {
                    _successIcon = GetTexture("YarnSpinnerSuccess");
                }
                return _successIcon;
            }
        }

        static Texture _failedIcon;
        public static Texture FailedIcon
        {
            get
            {
                if (_failedIcon == null)
                {
                    _failedIcon = GetTexture("YarnSpinnerFailed");
                }
                return _failedIcon;
            }
        }

        static Texture _notTestedIcon;
        public static Texture NotTestedIcon
        {
            get
            {
                if (_notTestedIcon == null)
                {
                    _notTestedIcon = GetTexture("YarnSpinnerNotTested");
                }
                return _notTestedIcon;
            }
        }

        static Texture _windowIcon;
        public static Texture WindowIcon
        {
            get
            {
                if (_windowIcon == null)
                {
                    _windowIcon = GetTexture("YarnSpinnerEditorWindow");
                }
                return _windowIcon;
            }
        }

        static Texture _logo;
        public static Texture Logo
        {
            get
            {
                if (_logo == null)
                {
                    _logo = GetTexture("YarnSpinnerLogo");
                }
                return _logo;
            }
        }
    }
}
