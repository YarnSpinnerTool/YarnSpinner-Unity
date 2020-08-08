using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
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
        // Detects when a YarnProgram has been imported (either created or
        // modified), and checks to see if its importer is configured to
        // associate it with a LocalizationDatabase. If it is, the
        // LocalizationDatabase is updated to include this program in its
        // TrackedPrograms collection. Finally, the LocalizationDatabase is
        // made to update its contents.
        //
        // We do this in a post-processor rather than in the importer
        // itself, because assets created in an importer don't actually
        // "exist" (as far as Unity is concerned) until after the import
        // process completes, so references to that asset aren't valid
        // (i.e. it hasn't been assigned a GUID yet).
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            // Get the list of paths to assets that we've just imported
            // whose main asset is a YarnProgram. (If there aren't any,
            // this method has nothing to do.)
            var importedYarnAssets = importedAssets.Where(path => AssetDatabase.GetMainAssetTypeAtPath(path)?.IsAssignableFrom(typeof(YarnProgram)) ?? false);

            if (importedYarnAssets.Count() == 0)
            {
                return;
            }

            // Tracks all databases that are affected by the  Yarn scripts
            // that we just imported
            var impactedDatabases = new HashSet<LocalizationDatabase>();

            foreach (var importedPath in importedYarnAssets)
            {
                var importer = AssetImporter.GetAtPath(importedPath) as YarnImporter;

                // Verify that we have the right kind of importer that we
                // expect.
                if (importer == null)
                {
                    Debug.Log($"{nameof(YarnProgram)} at {importedPath}'s importer is not a {nameof(YarnImporter)}. This probably indicates a problem elsewhere in your setup.");
                    continue;
                }

                // Try and get the localization database!
                var database = importer.localizationDatabase;

                if (database == null)
                {
                    // This program has no localization database to
                    // associate with, so nothing to do here.
                    continue;
                }

                var trackedPrograms = importer.localizationDatabase.TrackedPrograms;

                // The database is using data from this program if any of
                // the programs in its list have a path that match this
                // asset. (Doing it this way means we don't need to
                // deserialize the asset at importedPath unless we really
                // need to.)
                var databaseIsTrackingThisProgram = trackedPrograms.Select(p => AssetDatabase.GetAssetPath(p))
                                                                   .Contains(importedPath);

                if (databaseIsTrackingThisProgram == false)
                {
                    // The YarnProgram wants the database to have it in its
                    // TrackedPrograms list, but it's not there. We need to
                    // add it.

                    // First, import the program, so we can add the
                    // reference.
                    var program = AssetDatabase.LoadAssetAtPath<YarnProgram>(importedPath);

                    if (program == null)
                    {
                        // The program failed to be loaded at this path. It
                        // probably failed to compile, so we can't add it
                        // to the localized database.
                        continue;
                    }

                    // Add this program to the list.
                    importer.localizationDatabase.AddTrackedProgram(program);

                }

                // This database needs to be updated.
                impactedDatabases.Add(importer.localizationDatabase);
            }

            if (impactedDatabases.Count > 0)
            {
                foreach (var db in impactedDatabases)
                {
                    // Make the database update its contents
                    LocalizationDatabaseUtility.UpdateContents(db);
                }

                // Save any changed localization databases. (This will trigger
                // this method to be called again, but none of the paths that
                // we just worked with will have changed, so it won't trigger a
                // recursion.)
                AssetDatabase.SaveAssets();

            }
        }
    }

    /// <summary>
    /// A <see cref="ScriptedImporter"/> for Yarn assets. The actual asset used and referenced at runtime and in the editor will be a <see cref="YarnProgram"/>, which this class wraps around creating the asset's corresponding meta file.
    /// </summary>
    [ScriptedImporter(2, new[] { "yarn", "yarnc" })]
    public class YarnImporter : ScriptedImporter
    {
        // culture identifiers like en-US
        public string baseLanguageID;

        public string[] stringIDs;

        public bool AnyImplicitStringIDs => compilationStatus == Status.SucceededUntaggedStrings;
        public bool StringsAvailable => stringIDs?.Length > 0;

        public Status compilationStatus;

        public bool isSuccesfullyCompiled = false;

        public string compilationErrorMessage = null;

        public TextAsset baseLanguage;
        public YarnProgram.YarnTranslation[] localizations = new YarnProgram.YarnTranslation[0];
        public YarnProgram programContainer = default;

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
            OnValidate();
            var extension = System.IO.Path.GetExtension(ctx.assetPath);

            // Clear the list of strings, in case this compilation fails
            stringIDs = new string[] { };

            isSuccesfullyCompiled = false;

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
        private static string GetHashString(string inputString, int limitCharacters = -1)
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

            Yarn.Program compiledProgram = null;
            IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;

            compilationErrorMessage = null;

            try
            {
                // Compile the source code into a compiled Yarn program (or
                // generate a parse error)
                compilationStatus = Yarn.Compiler.Compiler.CompileString(sourceText, fileName, out compiledProgram, out stringTable);
                isSuccesfullyCompiled = true;
                compilationErrorMessage = string.Empty;
            }
            catch (Yarn.Compiler.ParseException e)
            {
                isSuccesfullyCompiled = false;
                compilationErrorMessage = e.Message;
                ctx.LogImportError(e.Message);                
            }

            // Create a container for storing the bytes
            if (programContainer == null)
            {
                programContainer = ScriptableObject.CreateInstance<YarnProgram>();
            }

            byte[] compiledBytes = null;

            if (compiledProgram != null)
            {
                using (var memoryStream = new MemoryStream())
                using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
                {
                    // Serialize the compiled program to memory
                    compiledProgram.WriteTo(outputStream);
                    outputStream.Flush();

                    compiledBytes = memoryStream.ToArray();
                }
            }


            programContainer.compiledProgram = compiledBytes;

            // Add this container to the imported asset; it will be
            // what the user interacts with in Unity
            ctx.AddObjectToAsset("Program", programContainer, YarnEditorUtility.GetYarnDocumentIconTexture());
            ctx.SetMainObject(programContainer);

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

                programContainer.baseLocalizationId = baseLanguageID;
                baseLanguage = textAsset;
                programContainer.localizations = localizations.Append(new YarnProgram.YarnTranslation(baseLanguageID, textAsset)).ToArray();
                programContainer.baseLocalizationId = baseLanguageID;

                stringIDs = lines.Select(l => l.ID).ToArray();
                
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

            isSuccesfullyCompiled = true;

            // Create a container for storing the bytes
            var programContainer = ScriptableObject.CreateInstance<YarnProgram>();
            programContainer.compiledProgram = bytes;

            // Add this container to the imported asset; it will be
            // what the user interacts with in Unity
            ctx.AddObjectToAsset("Program", programContainer);
            ctx.SetMainObject(programContainer);
        }
    }
}
