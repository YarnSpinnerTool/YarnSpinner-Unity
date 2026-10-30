namespace Yarn.Unity
{
    class PrepareBuild : UnityEditor.Build.BuildPlayerProcessor
    {
        public override void PrepareForBuild(UnityEditor.Build.BuildPlayerContext buildPlayerContext)
        {
            // This is called immediately before building starts. We calculate the
            // current version of the Yarn Spinner assembly, write it to a JSON file
            // somewhere on disk, and tell Unity to include that file with a given
            // name in the StreamingAssets folder inside the build.

            var version = typeof(DialogueRunner).Assembly.GetName().Version;
            var jsonText = $@"{{""note"":""This file exists to help us see which games are using Yarn Spinner. If you don't want it in your build, you can safely delete it."",""version"": ""{version}""}}";
            var path = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(path, jsonText);

            buildPlayerContext.AddAdditionalPathToStreamingAssets(path, "YarnSpinnerVersion.json");
        }
    }
}
