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

namespace Yarn.Unity
{

    internal class YarnAssetPostProcessor : AssetPostprocessor
    {
        // Detects when a YarnProject has been imported (either created or
        // modified), and checks to see if its importer is configured to
        // associate it with a LocalizationDatabase. If it is, the
        // LocalizationDatabase is updated to include this project in its
        // TrackedProjects collection. Finally, the LocalizationDatabase is
        // made to update its contents.
        //
        // We do this in a post-processor rather than in the importer
        // itself, because assets created in an importer don't actually
        // "exist" (as far as Unity is concerned) until after the import
        // process completes, so references to that asset aren't valid
        // (i.e. it hasn't been assigned a GUID yet).
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            // Find all localization databases whose list of recently
            // updated scripts has changed.
            var allLocalizationDatabases = AssetDatabase
                .FindAssets($"t:{nameof(LocalizationDatabase)}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(path))
                .Where(db => db.NeedsUpdate);

            if (allLocalizationDatabases.Count() == 0) {
                return;
            }

            foreach (var db in allLocalizationDatabases) {
                // Make the database update its contents
                LocalizationDatabaseUtility.UpdateContents(db);
            }

            // Save any changed localization databases. (This will
            // trigger this method to be called again, but the
            // localization databases will no longer need updating, so
            // we won't loop.)
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// A <see cref="ScriptedImporter"/> for Yarn assets. The actual asset used and referenced at runtime and in the editor will be a <see cref="YarnScript"/>, which this class wraps around creating the asset's corresponding meta file.
    /// </summary>
    [ScriptedImporter(2, new[] { "yarn", "yarnc" }, -1), HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnImporter : ScriptedImporter
    {
        // culture identifiers like en-US
        public string baseLanguageID;

        public string[] stringIDs;

        public bool AnyImplicitStringIDs;
        public bool StringsAvailable => stringIDs?.Length > 0;

        public bool isSuccesfullyParsed = false;

        public string parseErrorMessage = null;

        public TextAsset baseLanguage;
        
        [SerializeField]
        private YarnTranslation[] localizations = new YarnTranslation[0];

        public IEnumerable<YarnTranslation> ExternalLocalizations => localizations;
        public IEnumerable<YarnTranslation> AllLocalizations => localizations.Append(new YarnTranslation(baseLanguageID, baseLanguage));

        public void AddLocalization(string languageID, TextAsset languageAsset) {
            ArrayUtility.Add(ref localizations,  new YarnTranslation(languageID, languageAsset));
        }

        public LocalizationDatabase localizationDatabase;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(baseLanguageID))
            {
                baseLanguageID = DefaultLocalizationName;
            }
        }

        /// <summary>
        /// Returns the locale name to use as the base localization ID for
        /// a newly created Yarn script. This will be either the first
        /// entry in <see cref="ProjectSettings.TextProjectLanguages"/>, or
        /// if this is not set, the user's current culture.
        /// </summary>
        public static string DefaultLocalizationName
        {
            get
            {
                // If the user has added project wide text languages in the settings 
                // dialogue, we default to the first text language as base language
                if (ProjectSettings.TextProjectLanguages.Count > 0)
                {
                    return ProjectSettings.TextProjectLanguages[0];
                    
                }
                else
                {
                    // Otherwrise use system's language as base language
                    return CultureInfo.CurrentCulture.Name;
                }
            }
        }

        public YarnProject DestinationProject {
            get {
                var destinationProjectPath = AssetDatabase.FindAssets("t:YarnProject")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => AssetImporter.GetAtPath(path) as YarnProjectImporter)
                    .FirstOrDefault(importer => importer.sourceScripts.Select(s => AssetDatabase.GetAssetPath(s)).Contains(assetPath))?.assetPath;

                if (destinationProjectPath == null) {
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
            
            OnValidate();
            var extension = System.IO.Path.GetExtension(ctx.assetPath);

            // Clear the list of strings, in case this compilation fails
            stringIDs = new string[] { };

            isSuccesfullyParsed = false;

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

            var text = new TextAsset(File.ReadAllText( ctx.assetPath));

            // Add this container to the imported asset; it will be
            // what the user interacts with in Unity
            ctx.AddObjectToAsset("Program", text, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(text);

            Yarn.Program compiledProgram = null;
            IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;

            parseErrorMessage = null;

            try
            {
                // Compile the source code into a compiled Yarn program (or
                // generate a parse error)
                var compilationJob = CompilationJob.CreateFromString(fileName,  sourceText, null);
                compilationJob.CompilationType = CompilationJob.Type.StringsOnly;

                var result = Yarn.Compiler.Compiler.Compile(compilationJob);
                
                AnyImplicitStringIDs = result.ContainsImplicitStringTags;
                stringTable = result.StringTable;
                compiledProgram = result.Program;                
                isSuccesfullyParsed = true;
                parseErrorMessage = string.Empty;
            }
            catch (Yarn.Compiler.ParseException e)
            {
                isSuccesfullyParsed = false;
                parseErrorMessage = e.Message;
                ctx.LogImportError($"Error importing {ctx.assetPath}: {e.Message}");
                return;
            }

            // If there are lines in this script, generate a string table
            // asset for it
            if (stringTable?.Count > 0)
            {
                var lines = stringTable.Select(x => new StringTableEntry
                {
                    ID = x.Key,
                    Language = baseLanguageID,
                    Text = x.Value.text,
                    File = x.Value.fileName,
                    Node = x.Value.nodeName,
                    LineNumber = x.Value.lineNumber.ToString(),
                    Lock = GetHashString(x.Value.text, 8),
                }).OrderBy(entry => int.Parse(entry.LineNumber));

                var stringTableCSV = StringTableEntry.CreateCSV(lines);

                var textAsset = new TextAsset(stringTableCSV);
                textAsset.name = $"{fileName} ({baseLanguageID})";

                ctx.AddObjectToAsset("Strings", textAsset);

                // programContainer.baseLocalizationId = baseLanguageID;
                baseLanguage = textAsset;
                // programContainer.localizations = localizations.Append(new YarnScript.YarnTranslation(baseLanguageID, textAsset)).ToArray();
                // programContainer.baseLocalizationId = baseLanguageID;

                stringIDs = lines.Select(l => l.ID).ToArray();
                
            }

            if (localizationDatabase) {
                localizationDatabase.AddTrackedProject(AssetDatabase.AssetPathToGUID(ctx.assetPath));
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

            isSuccesfullyParsed = true;

            // Create a container for storing the bytes
            var programContainer = new TextAsset("<pre-compiled Yarn script>");
            
            // Add this container to the imported asset; it will be
            // what the user interacts with in Unity
            ctx.AddObjectToAsset("Program", programContainer, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(programContainer);
        }
    }
}
