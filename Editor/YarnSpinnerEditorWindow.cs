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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Yarn.Unity
{
    public class YarnSpinnerEditorWindow : EditorWindow
    {

        // Current scrolling position
        Vector2 scrollPos;

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
            this.titleContent.image = Icons.windowIcon;

            this.YarnSpinnerVersion = typeof(DialogueRunner).Assembly.GetName().Version.ToString();

            if (supportersText == null)
            {
                RequestSupporterText();
            }
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
        }
        
        SelectedMode selectedMode = 0;

        private string YarnSpinnerVersion;

        void OnGUI()
        {
            var modes = System.Enum.GetNames(typeof(SelectedMode));
            selectedMode = (SelectedMode)GUILayout.Toolbar((int)selectedMode, modes);

            switch (selectedMode)
            {
                case SelectedMode.About:
                    DrawAboutGUI();
                    break;
            }

        }

        void DrawAboutGUI()
        {

            float logoSize = Mathf.Min(EditorGUIUtility.currentViewWidth, 200);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            GUIStyle titleLabel = new GUIStyle(EditorStyles.largeLabel);
            titleLabel.fontSize = 20;
            titleLabel.alignment = TextAnchor.MiddleCenter;

            GUIStyle versionLabel = new GUIStyle(EditorStyles.largeLabel);
            versionLabel.fontSize = 12;
            versionLabel.alignment = TextAnchor.MiddleCenter;

            GUIStyle creditsLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
            creditsLabel.alignment = TextAnchor.MiddleCenter;
            creditsLabel.richText = true;


            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(logoSize)))
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(new GUIContent(Icons.logo), GUILayout.Width(logoSize), GUILayout.Height(logoSize));
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

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

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
        public static Texture successIcon
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
        public static Texture failedIcon
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
        public static Texture notTestedIcon
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
        public static Texture windowIcon
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
        public static Texture logo
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
