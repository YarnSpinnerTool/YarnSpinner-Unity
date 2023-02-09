using UnityEngine;
using Yarn.Unity;
using UnityEditor;

namespace MyGame
{

    public class Unity2019Test : MonoBehaviour
    {
        [YarnCommand]
        public void InstanceDemoActionWithNoName()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("instance_demo_action")]
        public void InstanceDemoAction()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("instance_demo_action_with_params")]
        public void InstanceDemoAction(int param)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand("instance_demo_action_with_optional_params")]
        public void InstanceDemoAction(int param, int param2 = 0)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand]
        public static void StaticDemoActionWithNoName()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("static_demo_action")]
        public static void StaticDemoAction()
        {
            Debug.Log($"Demo action!");
        }

        [YarnCommand("static_demo_action_with_params")]
        public static void StaticDemoAction(int param)
        {
            Debug.Log($"Demo action: {param}!");
        }

        [YarnCommand("static_demo_action_with_optional_params")]
        public static void StaticDemoAction(int param, int param2 = 0)
        {
            Debug.Log($"Demo action: {param}!");
        }


    }





    public class AnalysisDemo : ScriptableWizard
    {
        [MenuItem("Window/Yarn Spinner Analysis Demo")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<AnalysisDemo>("Analysis Demo", "Run");
            //If you don't want to use the secondary button simply leave it out:
            //ScriptableWizard.DisplayWizard<WizardCreateLight>("Create Light", "Create");

        }

        const string outputFilePath = "Assets/YarnActionRegistration.cs";

        void OnWizardCreate()
        {
            Debug.Log($"Analysis running");
            var analysis = new Yarn.Unity.ActionAnalyser.Analyser("Assets");
            try
            {
                var actions = analysis.GetActions();
                var source = analysis.GenerateRegistrationFileSource(actions);

                System.IO.File.WriteAllText(outputFilePath, source);
                UnityEditor.AssetDatabase.ImportAsset(outputFilePath);

                Debug.Log(source);
            }
            catch (Yarn.Unity.ActionAnalyser.AnalyserException e)
            {
                Debug.LogError($"Error generating source code: " + e.InnerException.ToString());
            }
        }

    }
}
