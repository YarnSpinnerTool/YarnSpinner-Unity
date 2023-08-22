using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor;

namespace Yarn.Unity.Example
{
    [ExecuteAlways]
    public class CinemachineInstallationCheck : MonoBehaviour
    {
        public void OnEnable()
        {
            var allLoadedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var hasCinemachine = false;
            foreach (var assembly in allLoadedAssemblies)
            {
                if (assembly.FullName.Contains("Cinemachine"))
                {
                    // Debug.Log(assembly.FullName);
                    hasCinemachine = true;
                    break;
                }
            }

            if (!hasCinemachine)
            {
                CinemachineInstaller.Install();
            }
        }
    }

    public class CinemachineInstaller : EditorWindow
    {
        public static void Install()
        {
            CinemachineInstaller window = EditorWindow.GetWindow<CinemachineInstaller>();
            window.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("This sample requires Cinemachine.");
            EditorGUILayout.LabelField("Without the package the sample won't work as intended.");

            if (GUILayout.Button("Install Cinemachine"))
            {
                PackageInstaller.Add();
                Close();
            }
        }
    }

    public static class PackageInstaller
    {
        static AddRequest Request;

        public static void Add()
        {
            Request = Client.Add("com.unity.cinemachine");
            EditorApplication.update += Progress;
        }

        static void Progress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"Unable to install cinemachine: {Request.Error.message}");
                }
                EditorApplication.update -= Progress;
            }
        }
    }
}
