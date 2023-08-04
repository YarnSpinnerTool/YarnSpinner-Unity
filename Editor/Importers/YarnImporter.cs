using UnityEngine;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Yarn;
using Yarn.Compiler;
using System.Security.Cryptography;
using System.Text;

namespace Yarn.Unity.Editor
{

    /// <summary>
    /// A <see cref="ScriptedImporter"/> for Yarn assets. The actual asset
    /// used and referenced at runtime and in the editor will be a <see
    /// cref="YarnScript"/>, which this class wraps around creating the
    /// asset's corresponding meta file.
    /// </summary>
    [ScriptedImporter(4, new[] { "yarn", "yarnc" }, -1), HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    [InitializeOnLoad]
    public class YarnImporter : ScriptedImporter
    {
        public IEnumerable<YarnProject> DestinationProjects
        {
            get
            {
                return DestinationProjectImporters
                    .Select(importer => AssetDatabase.LoadAssetAtPath<YarnProject>(AssetDatabase.GetAssetPath(importer)));
            }
        }

        public IEnumerable<YarnProjectImporter> DestinationProjectImporters
        {
            get
            {
                var myAssetPath = assetPath;
                var destinationProjectImporters = YarnEditorUtility.GetAllAssetsOf<YarnProjectImporter>("t:YarnProject")
                    .Where(importer =>
                    {
                        // Does this importer depend on this asset? If so,
                        // then this is our destination asset.
                        string[] dependencies = AssetDatabase.GetDependencies(importer.assetPath);
                        var importerDependsOnThisAsset = dependencies.Contains(myAssetPath);

                        return importerDependsOnThisAsset;
                    });
                return destinationProjectImporters;
            }
        }

        public bool HasErrors {
            get {
                foreach (var projectImporter in DestinationProjectImporters) {
                    if (projectImporter.GetErrorsForScript(ImportedScript).Any()) {
                        return true;
                    }
                }
                return false;
            }
        }

        private TextAsset ImportedScript => AssetDatabase.LoadAssetAtPath<TextAsset>(this.assetPath);

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("Import script " + ctx.assetPath);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var extension = System.IO.Path.GetExtension(ctx.assetPath);

            if (extension == ".yarn")
            {
                // Import this file as a TextAsset.
                var textAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
                ctx.AddObjectToAsset("Script", textAsset, YarnEditorUtility.GetYarnDocumentIconTexture());
                ctx.SetMainObject(textAsset);


                // Next, if we're a brand-new script, ensure that project
                // importers that need to depend on this script have a chance to
                // re-import.

                // Find all Yarn Project importers that _should_ be using this file.
                var projectsThatReferenceThisFile = YarnEditorUtility
                    .GetAllAssetsOf<YarnProjectImporter>("t:YarnProject")
                    .Where(importer => importer.GetProjectReferencesYarnFile(this));

                var missingProjectImporters = projectsThatReferenceThisFile
                    .Where(importer =>
                    {
                        var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(importer));
                        var importerDependsOnThisAsset = dependencies.Contains(ctx.assetPath);
                        return importerDependsOnThisAsset == false;
                    });

                // We now have a list of project importers that SHOULD be
                // depending on this script, but currently aren't (because this
                // script was created after the project was last imported.)

                // Re-import each project.
                foreach (var importer in missingProjectImporters)
                {
                    Debug.Log($"Project {importer.assetPath} needs to be reimported");
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }

            }
            else if (extension == ".yarnc")
            {
                ImportCompiledYarn(ctx);
            }
        }

        /// <summary>
        /// Returns a byte array containing a SHA-256 hash of <paramref
        /// name="inputString"/>.
        /// </summary>
        /// <param name="inputString">The string to produce a hash value
        /// for.</param>
        /// <returns>The hash of <paramref name="inputString"/>.</returns>
        private static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        /// <summary>
        /// Returns a string containing the hexadecimal representation of a
        /// SHA-256 hash of <paramref name="inputString"/>.
        /// </summary>
        /// <param name="inputString">The string to produce a hash
        /// for.</param>
        /// <param name="limitCharacters">The length of the string to
        /// return. The returned string will be at most <paramref
        /// name="limitCharacters"/> characters long. If this is set to -1,
        /// the entire string will be returned.</param>
        /// <returns>A string version of the hash.</returns>
        internal static string GetHashString(string inputString, int limitCharacters = -1)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("x2"));
            }

            if (limitCharacters == -1)
            {
                // Return the entire string
                return sb.ToString();
            }
            else
            {
                // Return a substring (or the entire string, if
                // limitCharacters is longer than the string)
                return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
            }
        }

        private void ImportCompiledYarn(AssetImportContext ctx)
        {

            var bytes = File.ReadAllBytes(ctx.assetPath);

            try
            {
                // Validate that this can be parsed as a Program protobuf
                var _ = Program.Parser.ParseFrom(bytes);
            }
            catch (Google.Protobuf.InvalidProtocolBufferException)
            {
                ctx.LogImportError("Invalid compiled yarn file. Please re-compile the source code.");
                return;
            }

            // Create a container for storing the bytes
            var programContainer = new TextAsset("<pre-compiled Yarn script>");

            // Add this container to the imported asset; it will be what
            // the user interacts with in Unity
            ctx.AddObjectToAsset("Program", programContainer, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(programContainer);
        }
    }
}
