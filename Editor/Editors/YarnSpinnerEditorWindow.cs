using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Reflection;
using System.Linq;

#nullable enable

namespace Yarn.Unity.Editor
{
    public class YarnSpinnerEditorWindow : EditorWindow
    {
        static UnityWebRequest? supportersRequest;
        static string supportersList = string.Empty;

        [SerializeField]
        private VisualTreeAsset? m_VisualTreeAsset = default;

        [InitializeOnLoadMethod]
        internal static void TryShowingWindowOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                // do a check to see if it is time to show
                if (ShouldShowAboutPage())
                {
                    ShowAbout();
                }
            };
        }

        [MenuItem("Window/Yarn Spinner/About Yarn Spinner... %#y", false, 2000)]
        public static void ShowAbout()
        {
            YarnSpinnerEditorWindow window = GetWindow<YarnSpinnerEditorWindow>(true, "About Yarn Spinner", true);
            window.minSize = new Vector2(512, 768);
            window.maxSize = window.minSize;
            window.Show();
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            if (m_VisualTreeAsset == null)
            {
                Debug.LogError("Unable to load the UXML file for the About Window");
                return;
            }

            // Instantiate UXML
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            // ok so will need to get lots of different pieces
            // core version, compiler version, unity version
            var coreLabel = root.Q<Label>("coreVLabel");
            var compLabel = root.Q<Label>("compVLabel");
            var unityLabel = root.Q<Label>("unityVLabel");

            coreLabel.text = $"Core: {GetInformationalVersionForType(typeof(Yarn.Dialogue))}";
            compLabel.text = $"Compiler: {GetInformationalVersionForType(typeof(Yarn.Compiler.Compiler))}";
            unityLabel.text = $"Unity: {GetInformationalVersionForType(typeof(Yarn.Unity.DialogueRunner))}";

            // docs link and discord link
            var resourcesContainer = root.Q("resources");
            var resourcesButtonContainer = resourcesContainer.Q("buttons");
            var dissy = resourcesButtonContainer.Q<Button>("support");
            var docs = resourcesButtonContainer.Q<Button>("docs");
            dissy.clicked += () =>
            {
                Application.OpenURL("https://discord.com/invite/yarnspinner");
            };
            docs.clicked += () =>
            {
                Application.OpenURL("https://docs.yarnspinner.dev/yarn-spinner-for-unity/");
            };

            var extrasContainer = root.Q("extras");
            var showcase = extrasContainer.Q<Button>("showcaseButton");
            var patreon = extrasContainer.Q<Button>("patreonButton");

            showcase.clicked += () =>
            {
                Application.OpenURL("https://www.yarnspinner.dev/tell-us");
            };

            patreon.clicked += () =>
            {
                Application.OpenURL("https://www.patreon.com/bePatron?u=11132340");
            };

            // list of patreons
            var patronsList = extrasContainer.Q<ScrollView>("patreonList");
            FillScrollList(patronsList);

            var review = extrasContainer.Q<Button>("reviewButton");
            var reviewLabel = extrasContainer.Q("reviewLabel");
            string reviewText;
            string reviewURL;

#pragma warning disable 162
            switch (Yarn.Unity.Editor.YarnPackageImporter.InstallationApproach)
            {
                case Yarn.Unity.Editor.YarnPackageImporter.InstallApproach.AssetStore:
                    review.style.display = DisplayStyle.Flex;
                    reviewLabel.style.display = DisplayStyle.Flex;
                    reviewText = "Review on the Asset Store";
                    reviewURL = "https://assetstore.unity.com/packages/tools/behavior-ai/yarn-spinner-for-unity-the-friendly-dialogue-and-narrative-tool-267061";
                    break;
                case Yarn.Unity.Editor.YarnPackageImporter.InstallApproach.Itch:
                    review.style.display = DisplayStyle.Flex;
                    reviewLabel.style.display = DisplayStyle.Flex;
                    reviewText = "Review at Itch";
                    reviewURL = "https://yarnspinner.itch.io/yarn-spinner/rate";
                    break;
                default:
                    review.style.display = DisplayStyle.None;
                    reviewLabel.style.display = DisplayStyle.None;
                    return;
            }

            review.text = reviewText;
            review.clicked += () =>
            {
                Application.OpenURL(reviewURL);
            };
#pragma warning restore
        }

        private void FillScrollList(ScrollView patronsList)
        {
            foreach (var child in patronsList.Children())
            {
                child.style.display = DisplayStyle.None;
            }

            if (string.IsNullOrEmpty(supportersList))
            {
                var label = new Label("Loading supporters...");
                patronsList.Add(label);
                RequestSupporterText();
            }
            else
            {
                var sponsors = supportersList.Split('\n');
                foreach (var sponsor in sponsors)
                {
                    var label = new Label(sponsor);
                    label.style.unityTextAlign = TextAnchor.MiddleCenter;
                    patronsList.Add(label);
                }
            }
        }

        private static string GetInformationalVersionForType(System.Type type)
        {
            var assembly = type.Assembly;

            var informationalVersionAttributes = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute),
                false) as AssemblyInformationalVersionAttribute[];

            var informationalVersionAttribute = informationalVersionAttributes.FirstOrDefault();

            string version = informationalVersionAttribute?.InformationalVersion ?? "<unknown version>";
            return version;
        }

        private static bool ShouldShowAboutPage()
        {
            // turns out that headless mode doesn't prevent windows being summoned
            if (Application.isBatchMode)
            {
                return false;
            }

            var version = Assembly.GetAssembly(typeof(DialogueRunner)).GetName().Version;

            var settings = YarnSpinnerProjectSettings.GetOrCreateSettings();
            var storedVersion = settings.Version;

            if (storedVersion.major != version.Major || storedVersion.minor != version.Minor)
            {
                settings.Version = (version.Major, version.Minor);
                settings.WriteSettings();
                return true;
            }

            return false;
        }


        private void EditorUpdate()
        {
            if (supportersRequest == null)
            {
                EditorApplication.update -= EditorUpdate;
                return;
            }
            if (!supportersRequest.isDone)
            {
                return;
            }

            // regardless of the success or failure of the process we want to unregister the editor update
            EditorApplication.update -= EditorUpdate;

            bool isError = supportersRequest.result != UnityWebRequest.Result.Success;

            if (isError)
            {
                supportersList = string.Empty;
            }
            else
            {
                supportersList = supportersRequest.downloadHandler.text;

                var extras = rootVisualElement.Q<VisualElement>("extras");
                var list = extras.Q<ScrollView>("patreonList");
                FillScrollList(list);
                list.MarkDirtyRepaint();
            }
        }
        private void RequestSupporterText()
        {
            // Start requesting the supporters text.
            supportersRequest = UnityWebRequest.Get("https://yarnspinner.dev/supporters.txt");
            supportersRequest.SendWebRequest();

            // Run EditorUpdate every editor frame so that we can handle
            // when the request ends.
            EditorApplication.update += EditorUpdate;
        }
    }
}