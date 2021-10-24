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
    [ScriptedImporter(3, new[] { "yarn", "yarnc" }, -1), HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    [InitializeOnLoad]
    public class YarnImporter : ScriptedImporter, IYarnErrorSource
    {
        static YarnImporter() => YarnPreventPlayMode.AddYarnErrorSourceType<YarnImporter>("t:TextAsset");

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained lines that did not have a line tag (and
        /// therefore were assigned an automatically-generated, 'implicit'
        /// string tag.) 
        /// </summary>
        public bool LastImportHadImplicitStringIDs;

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained any string tags.
        /// </summary>
        public bool LastImportHadAnyStrings;

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file was able to be parsed without errors. 
        /// </summary>
        /// <remarks>
        /// This value only represents whether syntactic errors exist or
        /// not. Other errors may exist that prevent this script from being
        /// compiled into a full program.
        /// </remarks>
        public bool isSuccessfullyParsed = false;

        /// <summary>
        /// Contains the text of the most recent parser error message.
        /// </summary>
        public List<string> parseErrorMessages = new List<string>();

        IList<string> IYarnErrorSource.CompileErrors => parseErrorMessages;

        bool IYarnErrorSource.Destroyed => this == null;

        public YarnProject DestinationProject
        {
            get
            {
                var myAssetPath = assetPath;
                var destinationProjectPath = YarnEditorUtility.GetAllAssetsOf<YarnProjectImporter>("t:YarnProject")
                    .FirstOrDefault(importer =>
                    {
                        // Does this importer depend on this asset? If so,
                        // then this is our destination asset.
                        string[] dependencies = AssetDatabase.GetDependencies(importer.assetPath);
                        var importerDependsOnThisAsset = dependencies.Contains(myAssetPath);

                        return importerDependsOnThisAsset;
                    })?.assetPath;

                if (destinationProjectPath == null)
                {
                    return null;
                }

                return AssetDatabase.LoadAssetAtPath<YarnProject>(destinationProjectPath);
            }
        }

        public override bool SupportsRemappedAssetType(System.Type type)
        {
            if (type.IsAssignableFrom(typeof(TextAsset)))
            {
                return true;
            }
            return false;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            YarnPreventPlayMode.AddYarnErrorSource(this);

            var extension = System.IO.Path.GetExtension(ctx.assetPath);

            // Clear the 'strings available' flags in case this import
            // fails
            LastImportHadAnyStrings = false;
            LastImportHadImplicitStringIDs = false;

            parseErrorMessages.Clear();

            isSuccessfullyParsed = false;

            if (extension == ".yarn")
            {
                ImportYarn(ctx);
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

        private void ImportYarn(AssetImportContext ctx)
        {
            var sourceText = File.ReadAllText(ctx.assetPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);

            var text = new TextAsset(File.ReadAllText(ctx.assetPath));

            // Add this container to the imported asset; it will be what
            // the user interacts with in Unity
            ctx.AddObjectToAsset("Program", text, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(text);

            Yarn.Program compiledProgram = null;
            IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;

            parseErrorMessages.Clear();

            // Compile the source code into a compiled Yarn program (or
            // generate a parse error)
            var compilationJob = CompilationJob.CreateFromString(fileName, sourceText, null);
            compilationJob.CompilationType = CompilationJob.Type.StringsOnly;

            var result = Yarn.Compiler.Compiler.Compile(compilationJob);

            IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                isSuccessfullyParsed = false;

                parseErrorMessages.AddRange(errors.Select(e => {
                    string message = $"{ctx.assetPath}: {e}";
                    ctx.LogImportError($"Error importing {message}");
                    return message;
                }));
            }
            else
            {
                isSuccessfullyParsed = true;
                LastImportHadImplicitStringIDs = result.ContainsImplicitStringTags;
                LastImportHadAnyStrings = result.StringTable.Count > 0;

                stringTable = result.StringTable;
                compiledProgram = result.Program;
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

            isSuccessfullyParsed = true;

            // Create a container for storing the bytes
            var programContainer = new TextAsset("<pre-compiled Yarn script>");

            // Add this container to the imported asset; it will be what
            // the user interacts with in Unity
            ctx.AddObjectToAsset("Program", programContainer, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(programContainer);
        }
    }
}
