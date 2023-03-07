using System.Collections.Generic;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using System.Linq;
using Yarn.Compiler;
using System.IO;

#if USE_UNITY_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

namespace Yarn.Unity.Editor
{
    [ScriptedImporter(3, new[] { "yarnproject" }, 1), HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    [InitializeOnLoad]
    public class YarnProjectImporter : ScriptedImporter, IYarnErrorSource
    {
        static YarnProjectImporter() => YarnPreventPlayMode.AddYarnErrorSourceType<YarnProjectImporter>("t:YarnProject");

        [System.Serializable]
        public class SerializedDeclaration
        {
            internal static List<Yarn.IType> BuiltInTypesList = new List<Yarn.IType> {
                Yarn.BuiltinTypes.String,
                Yarn.BuiltinTypes.Boolean,
                Yarn.BuiltinTypes.Number,
            };

            public string name = "$variable";

            [UnityEngine.Serialization.FormerlySerializedAs("type")]
            public string typeName = Yarn.BuiltinTypes.String.Name;

            public bool defaultValueBool;
            public float defaultValueNumber;
            public string defaultValueString;

            public string description;

            public bool isImplicit;

            public TextAsset sourceYarnAsset;

            public SerializedDeclaration(Declaration decl)
            {
                this.name = decl.Name;
                this.typeName = decl.Type.Name;
                this.description = decl.Description;
                this.isImplicit = decl.IsImplicit;

                sourceYarnAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(decl.SourceFileName);

                if (this.typeName == BuiltinTypes.String.Name) {
                    this.defaultValueString = System.Convert.ToString(decl.DefaultValue);
                } else if (this.typeName == BuiltinTypes.Boolean.Name) {
                    this.defaultValueBool = System.Convert.ToBoolean(decl.DefaultValue);
                } else if (this.typeName == BuiltinTypes.Number.Name) {
                    this.defaultValueNumber = System.Convert.ToSingle(decl.DefaultValue);
                } else {
                    throw new System.InvalidOperationException($"Invalid declaration type {decl.Type.Name}");
                }
            }
        }

        [System.Serializable]
        /// <summary>
        /// Pairs a language ID with a TextAsset.
        /// </summary>
        public class LanguageToSourceAsset
        {
            /// <summary>
            /// The locale ID that this translation should create a
            /// Localization for.
            /// </summary>
            [Language]
            public string languageID;

            /// <summary>
            /// The TextAsset containing CSV data that the Localization
            /// should use.
            /// </summary>
            // Hide this when its value is equal to whatever property is
            // stored in the YarnProjectImporterEditor class's
            // CurrentProjectDefaultLanguageProperty.
            [HideWhenPropertyValueEqualsContext(
                "languageID",
                typeof(YarnProjectImporterEditor),
                nameof(YarnProjectImporterEditor.CurrentProjectDefaultLanguageProperty),
                "Automatically included"
                )]
            [UnityEngine.Serialization.FormerlySerializedAs("stringsAsset")]
            public TextAsset stringsFile;

            /// <summary>
            /// The folder containing additional assets for the lines, such
            /// as voiceover audio files.
            /// </summary>
            public DefaultAsset assetsFolder;
        }

        public List<TextAsset> sourceScripts = new List<TextAsset>();

        public List<string> compileErrors = new List<string>();

        public List<SerializedDeclaration> serializedDeclarations = new List<SerializedDeclaration>();

        [Language]
        public string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        public List<LanguageToSourceAsset> languagesToSourceAssets;

        public bool useAddressableAssets;

        IList<string> IYarnErrorSource.CompileErrors => compileErrors;

        bool IYarnErrorSource.Destroyed => this == null;

#if USE_UNITY_LOCALIZATION
        public bool UseUnityLocalisationSystem = false;
        public StringTableCollection unityLocalisationStringTableCollection;
#endif

        public override void OnImportAsset(AssetImportContext ctx)
        {
#if YARNSPINNER_DEBUG
            UnityEngine.Profiling.Profiler.enabled = true;
#endif

            var project = ScriptableObject.CreateInstance<YarnProject>();

            project.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // Start by creating the asset - no matter what, we need to
            // produce an asset, even if it doesn't contain valid Yarn
            // bytecode, so that other assets don't lose their references.
            ctx.AddObjectToAsset("Project", project);
            ctx.SetMainObject(project);

            foreach (var script in sourceScripts)
            {
                string path = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrEmpty(path))
                {
                    // This is, for some reason, not a valid script we can
                    // use. Don't add a dependency on it.
                    ctx.LogImportError($"Error importing Yarn Project at {ctx.assetPath}: one of its source assets is missing");
                    return;
                }
                ctx.DependsOnSourceAsset(path);
            }

            // Parse declarations 
            var localDeclarationsCompileJob = CompilationJob.CreateFromFiles(ctx.assetPath);
            localDeclarationsCompileJob.CompilationType = CompilationJob.Type.DeclarationsOnly;

            var library = new Library();

            library = Actions.GetLibrary();
            localDeclarationsCompileJob.Library = library;

            IEnumerable<Declaration> localDeclarations;

            compileErrors.Clear();

            var result = Compiler.Compiler.Compile(localDeclarationsCompileJob);
            localDeclarations = result.Declarations;

            IEnumerable<Diagnostic> errors;
            
            errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                // We encountered errors while parsing for declarations.
                // Report them and exit.
                foreach (var error in errors)
                {
                    ctx.LogImportError($"Error in Yarn Project: {error}");
                    compileErrors.Add($"Error in Yarn Project {ctx.assetPath}: {error}");
                }

                return;
            }

            localDeclarations = localDeclarations
                .Where(decl => decl.Name.StartsWith("$Yarn.Internal") == false);

            // Store these so that we can continue displaying them after
            // this import step, in case there are compile errors later.
            // We'll replace this with a more complete list later if
            // compilation succeeds.
            serializedDeclarations = localDeclarations
                .Where(decl => !(decl.Type is FunctionType))
                .Select(decl => new SerializedDeclaration(decl)).ToList();

            // We're done processing this file - we've parsed it, and
            // pulled any information out of it that we need to. Now to
            // compile the scripts associated with this project.

            var scriptImporters = sourceScripts.Where(s => s != null).Select(s => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s)) as YarnImporter);

            // First step: check to see if there's any parse errors in the
            // files.
            var scriptsWithParseErrors = scriptImporters.Where(script => script.isSuccessfullyParsed == false);

            if (scriptsWithParseErrors.Count() != 0)
            {
                // Parse errors! We can't continue.
                string failingScriptNameList = string.Join("\n", scriptsWithParseErrors.Select(script => script.assetPath));
                compileErrors.Add($"Parse errors exist in the following files:\n{failingScriptNameList}");
                return;
            }

            // Get paths to the scripts we're importing, and also map them
            // to their corresponding importer
            var pathsToImporters = scriptImporters.ToDictionary(script => script.assetPath, script => script);

            if (pathsToImporters.Count == 0)
            {
                return; // nothing further to do here
            }

            // We now now compile!
            var job = CompilationJob.CreateFromFiles(pathsToImporters.Keys);
            job.VariableDeclarations = localDeclarations;

            job.Library = library;

            CompilationResult compilationResult;

            compilationResult = Compiler.Compiler.Compile(job);

            errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0) 
            {
                var errorGroups = errors.GroupBy(e => e.FileName);
                foreach (var errorGroup in errorGroups)
                {
                    var errorMessages = errorGroup.Select(e => e.ToString());

                    var asset = AssetDatabase.LoadAssetAtPath<Object>(errorGroup.Key);

                    foreach (var error in errorGroup)
                    {
                        ctx.LogImportError($"Error compiling <a href=\"{error.FileName}\">{error.FileName}</a> line {error.Range.Start.Line + 1}: {error.Message}", asset);
                    }

                    // Associate this compile error to the corresponding
                    // script's importer.
                    var importer = pathsToImporters[errorGroup.Key];
                    var path = errorGroup.Key;

                    compileErrors.AddRange(errorMessages);

                    importer.parseErrorMessages.AddRange(errorMessages);
                    EditorUtility.SetDirty(importer);
                }

                return;
            }

            if (compilationResult.Program == null)
            {
                ctx.LogImportError("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
                return;
            }

            // Store _all_ declarations - both the ones in this
            // .yarnproject file, and the ones inside the .yarn files.

            // While we're here, filter out any declarations that begin with our
            // Yarn internal prefix. These are synthesized variables that are
            // generated as a result of the compilation, and are not declared by
            // the user.
            serializedDeclarations = localDeclarations
                .Concat(compilationResult.Declarations)
                .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
                .Where(decl => !(decl.Type is FunctionType))
                .Select(decl => new SerializedDeclaration(decl)).ToList();

            // Clear error messages from all scripts - they've all passed
            // compilation
            foreach (var importer in pathsToImporters.Values)
            {
                importer.parseErrorMessages.Clear();
                EditorUtility.SetDirty(importer);
            }

#if USE_UNITY_LOCALIZATION
            if (UseUnityLocalisationSystem)
            {
                AddStringTableEntries(compilationResult, this.unityLocalisationStringTableCollection);
                project.localizationType = LocalizationType.Unity;
            } else {
                CreateYarnInternalLocalizationAssets(ctx, project, compilationResult);
                project.localizationType = LocalizationType.YarnInternal;
            }
#else
            CreateYarnInternalLocalizationAssets(ctx, project, compilationResult);
            project.localizationType = LocalizationType.YarnInternal;
#endif

            // Store the compiled program
            byte[] compiledBytes = null;

            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
            {
                // Serialize the compiled program to memory
                compilationResult.Program.WriteTo(outputStream);
                outputStream.Flush();

                compiledBytes = memoryStream.ToArray();
            }

            project.compiledYarnProgram = compiledBytes;

#if YARNSPINNER_DEBUG
            UnityEngine.Profiling.Profiler.enabled = false;
#endif

        }

        private void CreateYarnInternalLocalizationAssets(AssetImportContext ctx, YarnProject project, CompilationResult compilationResult)
        {
            // Will we need to create a default localization? This variable
            // will be set to false if any of the languages we've
            // configured in languagesToSourceAssets is the default
            // language.
            var shouldAddDefaultLocalization = true;

            foreach (var pair in languagesToSourceAssets)
            {
                // Don't create a localization if the language ID was not
                // provided
                if (string.IsNullOrEmpty(pair.languageID))
                {
                    Debug.LogWarning($"Not creating a localization for {project.name} because the language ID wasn't provided. Add the language ID to the localization in the Yarn Project's inspector.");
                    continue;
                }

                IEnumerable<StringTableEntry> stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (pair.languageID == defaultLanguage)
                {
                    // We'll use the program-supplied string table.
                    stringTable = GenerateStringsTable();

                    // We don't need to add a default localization.
                    shouldAddDefaultLocalization = false;
                }
                else
                {
                    try
                    {
                        if (pair.stringsFile == null)
                        {
                            // We can't create this localization because we
                            // don't have any data for it.
                            Debug.LogWarning($"Not creating a localization for {pair.languageID} in the Yarn Project {project.name} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                            continue;
                        }

                        stringTable = StringTableEntry.ParseFromCSV(pair.stringsFile.text);
                    }
                    catch (System.ArgumentException e)
                    {
                        Debug.LogWarning($"Not creating a localization for {pair.languageID} in the Yarn Project {project.name} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                }

                var newLocalization = ScriptableObject.CreateInstance<Localization>();
                newLocalization.LocaleCode = pair.languageID;

                // Add these new lines to the localisation's asset
                foreach (var entry in stringTable) {
                    newLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }


                project.localizations.Add(newLocalization);
                newLocalization.name = pair.languageID;

                if (pair.assetsFolder != null)
                {
                    var assetsFolderPath = AssetDatabase.GetAssetPath(pair.assetsFolder);

                    if (assetsFolderPath == null)
                    {
                        // This was somehow not a valid reference?
                        Debug.LogWarning($"Can't find assets for localization {pair.languageID} in {project.name} because a path for the provided assets folder couldn't be found.");
                    }
                    else
                    {
                        newLocalization.ContainsLocalizedAssets = true;

#if USE_ADDRESSABLES
                        const bool addressablesAvailable = true;
#else
                        const bool addressablesAvailable = false;
#endif

                        if (addressablesAvailable && useAddressableAssets)
                        {
                            // We only need to flag that the assets
                            // required by this localization are accessed
                            // via the Addressables system. (Call
                            // YarnProjectUtility.UpdateAssetAddresses to
                            // ensure that the appropriate assets have the
                            // appropriate addresses.)
                            newLocalization.UsesAddressableAssets = true;
                        }
                        else
                        {
                            // We need to find the assets used by this
                            // localization now, and assign them to the
                            // Localization object.
#if YARNSPINNER_DEBUG
                            // This can take some time, so we'll measure
                            // how long it takes.
                            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                            // Get the line IDs.
                            IEnumerable<string> lineIDs = stringTable.Select(s => s.ID);

                            // Map each line ID to its asset path.
                            var stringIDsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, assetsFolderPath);

                            // Load the asset, so we can assign the reference.
                            var assetPaths = stringIDsToAssetPaths
                                .Select(a => new KeyValuePair<string, Object>(a.Key, AssetDatabase.LoadAssetAtPath<Object>(a.Value)));

                            newLocalization.AddLocalizedObjects(assetPaths);

#if YARNSPINNER_DEBUG
                            stopwatch.Stop();
                            Debug.Log($"Imported {stringIDsToAssetPaths.Count()} assets for {project.name} \"{pair.languageID}\" in {stopwatch.ElapsedMilliseconds}ms");
#endif
                        }
                    }
                }

                ctx.AddObjectToAsset("localization-" + pair.languageID, newLocalization);

                if (pair.languageID == defaultLanguage)
                {
                    // If this is our default language, set it as such
                    project.baseLocalization = newLocalization;

                    // Since this is the default language, also populate the line metadata.
                    project.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
                }
                else
                {
                    // This localization depends upon a source asset. Make
                    // this asset get re-imported if this source asset was
                    // modified
                    ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(pair.stringsFile));
                }
            }

            if (shouldAddDefaultLocalization)
            {
                // We didn't add a localization for the default language.
                // Create one for it now.
                var stringTableEntries = GetStringTableEntries(compilationResult);

                var developmentLocalization = ScriptableObject.CreateInstance<Localization>();
                developmentLocalization.name = $"Default ({defaultLanguage})";
                developmentLocalization.LocaleCode = defaultLanguage;


                // Add these new lines to the development localisation's asset
                foreach (var entry in stringTableEntries)
                {
                    developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }

                project.baseLocalization = developmentLocalization;
                project.localizations.Add(project.baseLocalization);
                ctx.AddObjectToAsset("default-language", developmentLocalization);

                // Since this is the default language, also populate the line metadata.
                project.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
            }
        }

#if USE_UNITY_LOCALIZATION
        private void AddStringTableEntries(CompilationResult compilationResult, StringTableCollection unityLocalisationStringTableCollection)
        {
            if (unityLocalisationStringTableCollection == null)
            {
                Debug.LogError("Unable to generate String Table Entries as the string collection is null");
                return;
            }

            var defaultCulture = new System.Globalization.CultureInfo(defaultLanguage);

            foreach (var table in unityLocalisationStringTableCollection.StringTables)
            {
                if (table.LocaleIdentifier.CultureInfo != defaultCulture)
                {
                    var neutralTable = table.LocaleIdentifier.CultureInfo.IsNeutralCulture 
                        ? table.LocaleIdentifier.CultureInfo 
                        : table.LocaleIdentifier.CultureInfo.Parent;

                    var defaultNeutral = defaultCulture.IsNeutralCulture 
                        ? defaultCulture 
                        : defaultCulture.Parent;

                    if (!neutralTable.Equals(defaultNeutral))
                    {
                        continue;
                    }
                }

                foreach (var entry in compilationResult.StringTable)
                {
                    var lineID = entry.Key;
                    var stringInfo = entry.Value;

                    var lineEntry = table.AddEntry(lineID, stringInfo.text);

                    var existingMetadata = lineEntry.GetMetadata<UnityLocalization.LineMetadata>();

                    if (existingMetadata != null) {
                        lineEntry.RemoveMetadata(existingMetadata);
                    }

                    lineEntry.AddMetadata(new UnityLocalization.LineMetadata
                    {
                        nodeName = stringInfo.nodeName,
                        tags = RemoveLineIDFromMetadata(stringInfo.metadata).ToArray(),
                    });
                }
                return;
            }
            Debug.LogWarning($"Unable to find a locale in the string table that matches the default locale {defaultLanguage}");
        }
#endif

        /// <summary>
        /// Gets a value indicating whether this Yarn Project is able to
        /// generate a strings table - that is, it has no compile errors,
        /// it has at least one script, and all scripts are fully tagged.
        /// </summary>
        /// <inheritdoc path="exception"
        /// cref="GetScriptHasLineTags(TextAsset)"/>
        internal bool CanGenerateStringsTable => this.compileErrors.Count == 0 && sourceScripts.Count > 0 && sourceScripts.All(s => GetScriptHasLineTags(s));

        /// <summary>
        /// Gets a value indicating whether the source script has line
        /// tags.
        /// </summary>
        /// <param name="script">The source script to add. This script must
        /// have been imported by a <see cref="YarnImporter"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the the script is fully tagged, <see
        /// langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="script"/> is <see
        /// langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="script"/> is not imported by a <see
        /// cref="YarnImporter"/>.
        /// </exception>
        private bool GetScriptHasLineTags(TextAsset script)
        {
            if (script == null)
            {
                // This might be a 'None' or 'Missing' asset, so return
                // false here.
                return false;
            }

            // Get the importer for this TextAsset
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(script)) as YarnImporter;

            if (importer == null)
            {
                throw new System.ArgumentException($"The asset {script} is not imported via a {nameof(YarnImporter)}");
            }

            // Did it have any implicit string IDs when it was imported?
            return importer.LastImportHadImplicitStringIDs == false;
        }

        private CompilationResult? CompileStringsOnly()
        {
            var pathsToImporters = sourceScripts.Where(s => s != null).Select(s => AssetDatabase.GetAssetPath(s));

            if (pathsToImporters.Count() == 0)
            {
                // We have no scripts to work with.
                return null;
            }

            // We now now compile!
            var job = CompilationJob.CreateFromFiles(pathsToImporters);
            job.CompilationType = CompilationJob.Type.StringsOnly;

            return Compiler.Compiler.Compile(job);
        }

        /// <summary>
        /// Generates a collection of <see cref="StringTableEntry"/>
        /// objects, one for each line in this Yarn Project's scripts.
        /// </summary>
        /// <returns>An IEnumerable containing a <see
        /// cref="StringTableEntry"/> for each of the lines in the Yarn
        /// Project, or <see langword="null"/> if the Yarn Project contains
        /// errors.</returns>
        internal IEnumerable<StringTableEntry> GenerateStringsTable()
        {
            CompilationResult? compilationResult = CompileStringsOnly();

            if (!compilationResult.HasValue)
            {
                // We only get no value if we have no scripts to work with.
                // In this case, return an empty collection - there's no
                // error, but there's no content either.
                return new List<StringTableEntry>();
            }

            var errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                Debug.LogError($"Can't generate a strings table from a Yarn Project that contains compile errors", null);
                return null;
            }

            return GetStringTableEntries(compilationResult.Value);
        }

        internal IEnumerable<LineMetadataTableEntry> GenerateLineMetadataEntries()
        {
            CompilationResult? compilationResult = CompileStringsOnly();

            if (!compilationResult.HasValue)
            {
                // We only get no value if we have no scripts to work with.
                // In this case, return an empty collection - there's no
                // error, but there's no content either.
                return new List<LineMetadataTableEntry>();
            }

            var errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                Debug.LogError($"Can't generate line metadata entries from a Yarn Project that contains compile errors", null);
                return null;
            }

            return LineMetadataTableEntriesFromCompilationResult(compilationResult.Value);
        }

        private IEnumerable<StringTableEntry> GetStringTableEntries(CompilationResult result)
        {
            
            return result.StringTable.Select(x => new StringTableEntry
            {
                ID = x.Key,
                Language = defaultLanguage,
                Text = x.Value.text,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Lock = YarnImporter.GetHashString(x.Value.text, 8),
                Comment = GenerateCommentWithLineMetadata(x.Value.metadata),
            });
        }

        private IEnumerable<LineMetadataTableEntry> LineMetadataTableEntriesFromCompilationResult(CompilationResult result)
        {
            return result.StringTable.Select(x => new LineMetadataTableEntry
            {
                ID = x.Key,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray(),
            }).Where(x => x.Metadata.Length > 0);
        }

        /// <summary>
        /// Generates a string with the line metadata. This string is intended
        /// to be used in the "comment" column of a strings table CSV. Because
        /// of this, it will ignore the line ID if it exists (which is also
        /// part of the line metadata).
        /// </summary>
        /// <param name="metadata">The metadata from a given line.</param>
        /// <returns>A string prefixed with "Line metadata: ", followed by each
        /// piece of metadata separated by whitespace. If no metadata exists or
        /// only the line ID is part of the metadata, returns an empty string
        /// instead.</returns>
        private string GenerateCommentWithLineMetadata(string[] metadata)
        {
            var cleanedMetadata = RemoveLineIDFromMetadata(metadata);

            if (cleanedMetadata.Count() == 0)
            {
                return string.Empty;
            }

            return $"Line metadata: {string.Join(" ", cleanedMetadata)}";
        }

        /// <summary>
        /// Removes any line ID entry from an array of line metadata.
        /// Line metadata will always contain a line ID entry if it's set. For
        /// example, if a line contains "#line:1eaf1e55", its line metadata
        /// will always have an entry with "line:1eaf1e55".
        /// </summary>
        /// <param name="metadata">The array with line metadata.</param>
        /// <returns>An IEnumerable with any line ID entries removed.</returns>
        private IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
        {
            return metadata.Where(x => !x.StartsWith("line:"));
        }
    }
}
