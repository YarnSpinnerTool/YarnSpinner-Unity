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
using UnityEditorInternal;
using System.Collections;

namespace Yarn.Unity
{
    [ScriptedImporter(2, new[] { "yarnproject" }, 1), HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnProjectImporter : ScriptedImporter
    {

        [System.Serializable]
        public class SerializedDeclaration
        {
            public string name = "$variable";
            public Yarn.Type type = Yarn.Type.String;
            public bool defaultValueBool;
            public float defaultValueNumber;
            public string defaultValueString;

            public string description;

            public TextAsset sourceYarnAsset;

            public SerializedDeclaration(Declaration decl)
            {
                this.name = decl.Name;
                this.type = decl.ReturnType;
                this.description = decl.Description;

                sourceYarnAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(decl.SourceFileName);

                switch (this.type)
                {
                    case Type.Number:
                        this.defaultValueNumber = (float)decl.DefaultValue;
                        break;
                    case Type.String:
                        this.defaultValueString = (string)decl.DefaultValue;
                        break;
                    case Type.Bool:
                        this.defaultValueBool = (bool)decl.DefaultValue;
                        break;
                    default:
                        throw new System.InvalidOperationException($"Invalid declaration type {decl.ReturnType}");
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

        public string compileError;
        public List<SerializedDeclaration> serializedDeclarations = new List<SerializedDeclaration>();

        [Language]
        public string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        public List<LanguageToSourceAsset> languagesToSourceAssets;

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
                    continue;
                }
                ctx.DependsOnSourceAsset(path);
            }

            // Parse declarations 
            var localDeclarationsCompileJob = CompilationJob.CreateFromFiles(ctx.assetPath);
            localDeclarationsCompileJob.CompilationType = CompilationJob.Type.DeclarationsOnly;

            IEnumerable<Declaration> localDeclarations;

            compileError = null;

            try
            {
                var result = Compiler.Compiler.Compile(localDeclarationsCompileJob);
                localDeclarations = result.Declarations;
            }
            catch (ParseException e)
            {
                ctx.LogImportError($"Error in Yarn Project: {e.Message}");
                compileError = $"Error in Yarn Project {ctx.assetPath}: {e.Message}";
                return;
            }

            // Store these so that we can continue displaying them after
            // this import step, in case there are compile errors later.
            // We'll replace this with a more complete list later if
            // compilation succeeds.
            serializedDeclarations = localDeclarations
                .Where(decl => decl.DeclarationType == Declaration.Type.Variable)
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
                compileError = $"Parse errors exist in the following files:\n{failingScriptNameList}";
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

            CompilationResult compilationResult;

            try
            {
                compilationResult = Compiler.Compiler.Compile(job);
            }
            catch (TypeException e)
            {
                ctx.LogImportError($"Error compiling: {e.Message}");
                compileError = e.Message;

                var importer = pathsToImporters[e.FileName];
                importer.parseErrorMessage = e.Message;
                EditorUtility.SetDirty(importer);

                return;
            }
            catch (ParseException e)
            {
                ctx.LogImportError(e.Message);
                compileError = e.Message;

                var importer = pathsToImporters[e.FileName];
                importer.parseErrorMessage = e.Message;
                EditorUtility.SetDirty(importer);

                return;
            }

            if (compilationResult.Program == null)
            {
                ctx.LogImportError("Internal error: Failed to compile: resulting program was null, but compiler did not throw a parse exception.");
                return;
            }

            // Store _all_ declarations - both the ones in this
            // .yarnproject file, and the ones inside the .yarn files
            serializedDeclarations = localDeclarations
                .Concat(compilationResult.Declarations)
                .Where(decl => decl.DeclarationType == Declaration.Type.Variable)
                .Select(decl => new SerializedDeclaration(decl)).ToList();

            // Clear error messages from all scripts - they've all passed
            // compilation
            foreach (var importer in pathsToImporters.Values)
            {
                importer.parseErrorMessage = null;
            }

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
                        if (pair.stringsFile == null) {
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

                newLocalization.AddLocalizedStrings(stringTable);

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
                        var stringIDsToAssets = FindAssetsForLineIDs(stringTable.Select(s => s.ID), assetsFolderPath);

#if YARNSPINNER_DEBUG
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                        newLocalization.AddLocalizedObjects(stringIDsToAssets.AsEnumerable());

#if YARNSPINNER_DEBUG
                        stopwatch.Stop();
                        Debug.Log($"Imported {stringIDsToAssets.Count()} assets for {project.name} \"{pair.languageID}\" in {stopwatch.ElapsedMilliseconds}ms");
#endif
                    }
                }

                ctx.AddObjectToAsset("localization-" + pair.languageID, newLocalization);


                if (pair.languageID == defaultLanguage) {
                    // If this is our default language, set it as such
                    project.baseLocalization = newLocalization;
                } else {
                    // This localization depends upon a source asset. Make
                    // this asset get re-imported if this source asset was
                    // modified
                    ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(pair.stringsFile));
                }


            }

            if (shouldAddDefaultLocalization) {
                // We didn't add a localization for the default language.
                // Create one for it now.

                var developmentLocalization = ScriptableObject.CreateInstance<Localization>();

                developmentLocalization.LocaleCode = defaultLanguage;

                var stringTableEntries = compilationResult.StringTable.Select(x => new StringTableEntry
                {
                    ID = x.Key,
                    Language = defaultLanguage,
                    Text = x.Value.text,
                    File = x.Value.fileName,
                    Node = x.Value.nodeName,
                    LineNumber = x.Value.lineNumber.ToString(),
                    Lock = YarnImporter.GetHashString(x.Value.text, 8),
                });

                developmentLocalization.AddLocalizedStrings(stringTableEntries);

                project.baseLocalization = developmentLocalization;

                project.localizations.Add(project.baseLocalization);

                developmentLocalization.name = $"Default ({defaultLanguage})";

                ctx.AddObjectToAsset("default-language", developmentLocalization);
            }

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

        private static Dictionary<string, Object> FindAssetsForLineIDs(IEnumerable<string> lineIDs, string assetsFolderPath)
        {
            // Find _all_ files in this director that are not .meta files
            var allFiles = Directory.EnumerateFiles(assetsFolderPath, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".meta") == false);

            // Match files with those whose filenames contain a line ID
            var matchedFilesAndPaths = lineIDs.GroupJoin(
                // the elements we're matching lineIDs to
                allFiles,
                // the key for lineIDs (being strings, it's just the line
                // ID itself)
                lineID => lineID,
                // the key for assets (the filename without the path)
                assetPath => Path.GetFileName(assetPath),
                // the way we produce the result (a key-value pair)
                (lineID, assetPaths) =>
                {
                    if (assetPaths.Count() > 1)
                    {
                        Debug.LogWarning($"Line {lineID} has {assetPaths.Count()} possible assets.\n{string.Join(", ", assetPaths)}");
                    }
                    return new { lineID, assetPaths };
                },
                // the way we test to see if two elements should be joined
                // (does the filename contain the line ID?)
                Compare.By<string>((fileName, lineID) =>
                {
                    var lineIDWithoutPrefix = lineID.Replace("line:", "");
                    return fileName.Contains(lineIDWithoutPrefix);
                })
                )
                .ToDictionary(entry => entry.lineID, entry => AssetDatabase.LoadAssetAtPath<Object>(entry.assetPaths.FirstOrDefault()));


            return matchedFilesAndPaths;
        }

        /// <summary>
        /// Gets a value indicating whether this Yarn Project is able to
        /// generate a strings table - that is, it has no compile errors,
        /// it has at least one script, and all scripts are fully tagged.
        /// </summary>
        /// <inheritdoc path="exception"
        /// cref="GetScriptHasLineTags(TextAsset)"/>
        internal bool CanGenerateStringsTable => string.IsNullOrEmpty(this.compileError) && sourceScripts.Count > 0 && sourceScripts.All(s => GetScriptHasLineTags(s));

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
            var pathsToImporters = sourceScripts.Where(s => s != null).Select(s => AssetDatabase.GetAssetPath(s));

            if (pathsToImporters.Count() == 0)
            {
                // We have no scripts to work with - return an empty
                // collection - there's no error, but there's no content
                // either
                return new List<StringTableEntry>();
            }

            // We now now compile!
            var job = CompilationJob.CreateFromFiles(pathsToImporters);
            job.CompilationType = CompilationJob.Type.StringsOnly;

            CompilationResult compilationResult;

            try
            {
                compilationResult = Compiler.Compiler.Compile(job);
            }
            catch (ParseException)
            {
                Debug.LogError($"Can't generate a strings table from a Yarn Project that contains compile errors", null);
                return null;
            }

            IEnumerable<StringTableEntry> stringTableEntries = compilationResult.StringTable.Select(x => new StringTableEntry
            {
                ID = x.Key,
                Language = defaultLanguage,
                Text = x.Value.text,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Lock = YarnImporter.GetHashString(x.Value.text, 8),
            });

            return stringTableEntries;


        }
    }

    public class ReorderableDeclarationsList
    {
        private struct Problem
        {
            public string text;
            public int index;
        }

        public bool IsSearching => string.IsNullOrEmpty(filterString) == false;
        public bool HasProblems => problems.Count > 0;

        private ReorderableList _list;
        private string filterString;

        private List<int> filteredIndices = new List<int>();
        private List<Problem> problems = new List<Problem>();

        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;

        public ReorderableDeclarationsList(SerializedObject serializedObject, SerializedProperty property)
        {
            this.serializedObject = serializedObject;
            serializedProperty = property;

            _list = new UnityEditorInternal.ReorderableList(serializedObject, property, false, true, true, true)
            {
                drawHeaderCallback = (rect) => DrawListHeader(rect, "Declarations"),
                drawElementCallback = (rect, index, isActive, isFocused) => DrawListElement(rect, index, isActive, isFocused, useSearch: true),
                elementHeightCallback = (index) => GetElementHeight(index, useSearch: true),
                onAddCallback = OnAdd,
                onCanAddCallback = OnCanAdd,
                onCanRemoveCallback = OnCanRemove,
            };

            UpdateProblems();
        }

        private void OnAdd(ReorderableList list)
        {
            serializedProperty.InsertArrayElementAtIndex(serializedProperty.arraySize);
            var entry = serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1);

            // Clear necessary properties to something useful
            var nameProp = entry.FindPropertyRelative("name");
            var typeProp = entry.FindPropertyRelative("type");
            var defaultValueStringProp = entry.FindPropertyRelative("defaultValueString");
            var sourceYarnAssetProp = entry.FindPropertyRelative("sourceYarnAsset");
            var descriptionProp = entry.FindPropertyRelative("description");

            nameProp.stringValue = "$variable";
            typeProp.enumValueIndex = (int)Yarn.Type.String;
            defaultValueStringProp.stringValue = string.Empty;
            sourceYarnAssetProp.objectReferenceValue = null;
            descriptionProp.stringValue = string.Empty;

        }

        private void UpdateProblems()
        {
            problems.Clear();

            var declarations = Cast<SerializedProperty>(serializedProperty.GetEnumerator()).ToList();

            // Find all variables with duplicate names
            var names = declarations.Select((p, index) => new { name = p.FindPropertyRelative("name").stringValue, index });

            var duplicateNames = names.GroupBy(s => s.name)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

            problems.AddRange(duplicateNames.Select(name => new Problem
            {
                text = $"Duplicate variable name {name}",
                index = names.First(n => n.name == name).index
            }));

            var invalidVariableNames = names.Where(decl => decl.name.Equals(string.Empty) == false && !decl.name.StartsWith("$"));

            problems.AddRange(invalidVariableNames.Select(decl => new Problem { text = $"Variable name '{decl.name}' must begin with a $", index = decl.index }));

            var emptyVariableNames = names.Where(name => string.IsNullOrEmpty(name.name));

            problems.AddRange(emptyVariableNames.Select(decl => new Problem { text = $"Variable name must not be empty", index = decl.index }));

        }

        IEnumerable<T> Cast<T>(IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T)iterator.Current;
            }
        }

        private bool OnCanRemove(ReorderableList list)
        {
            var isFromScript = serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null;
            return IsSearching == false && !isFromScript;
        }

        private bool OnCanAdd(ReorderableList list)
        {
            return IsSearching == false;
        }

        private bool ShouldShowElement(int index)
        {
            // If we're not searching then all indices are shown
            if (IsSearching == false)
            {
                return true;
            }

            // Otherwise, we show this element if its index is in the
            // filtered list
            return filteredIndices.Contains(index);
        }

        private float GetElementHeight(int index, bool useSearch)
        {
            if (useSearch == false || ShouldShowElement(index))
            {
                var item = serializedProperty.GetArrayElementAtIndex(index);
                return DeclarationPropertyDrawer.GetPropertyHeightImpl(item, null);
            }
            else
            {
                return 0;
            }
        }

        private void DrawListHeader(Rect rect, string label)
        {
            GUI.Label(rect, label);
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused, bool useSearch)
        {
            if (useSearch == false || ShouldShowElement(index))
            {
                var item = serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, item);
            }
        }

        public void DrawLayout()
        {
            //serializedObject.Update();
            EditorGUILayout.Space();

            foreach (var problem in problems)
            {
                if (problem.index != -1)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                        {
                            _list.index = problem.index;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                }
                EditorGUILayout.Space();
            }

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                filterString = EditorGUILayout.TextField("Search", filterString);

                if (changeCheck.changed)
                {
                    UpdateSearch(filterString);
                }
            }


            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                _list.DoLayoutList();

                if (changeCheck.changed)
                {
                    UpdateProblems();
                }
            }


            if (IsSearching && filteredIndices.Count == 0)
            {
                EditorGUILayout.LabelField("No items to show.");
            }
        }

        private void UpdateSearch(string filterString)
        {
            filteredIndices.Clear();

            if (string.IsNullOrEmpty(filterString) == false)
            {

                var count = serializedProperty.arraySize;
                for (int i = 0; i < count; i++)
                {


                    var item = serializedProperty.GetArrayElementAtIndex(i);

                    var nameProperty = item.FindPropertyRelative("name");
                    if (nameProperty.stringValue?.Contains(filterString) ?? false)
                    {
                        filteredIndices.Add(i);
                    }
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(YarnProjectImporter.SerializedDeclaration))]
    public class DeclarationPropertyDrawer : PropertyDrawer
    {

        /// <summary>
        /// Draws either a property field or a label field for <paramref
        /// name="property"/> at <paramref name="position"/>, depending on
        /// the value of <paramref name="readOnly"/>.
        /// </summary>
        /// <param name="position">The rectangle in which to draw the
        /// control.</param>
        /// <param name="property">The property to draw a control
        /// for.</param>
        /// <param name="readOnly">Whether the property is read-only or
        /// not.</param>
        private void DrawPropertyField(Rect position, SerializedProperty property, bool readOnly, string label = null)
        {
            if (label == null)
            {
                label = property.displayName;
            }
            if (readOnly)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        EditorGUI.LabelField(position, label, property.intValue.ToString());
                        break;
                    case SerializedPropertyType.Boolean:
                        EditorGUI.Toggle(position, label, property.boolValue);
                        break;
                    case SerializedPropertyType.Float:
                        EditorGUI.LabelField(position, label, property.floatValue.ToString());
                        break;
                    case SerializedPropertyType.String:
                        EditorGUI.LabelField(position, label, property.stringValue);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUI.ObjectField(position, property);
                        }
                        break;
                    case SerializedPropertyType.Enum:
                        var displayValue = property.enumDisplayNames[property.enumValueIndex];
                        EditorGUI.LabelField(position, label, displayValue);
                        break;
                }
            }
            else
            {
                // Use delayed fields where possible to preserve
                // responsivity (we don't want to force a serialization on
                // Unity 2018 after every keystroke, and delayed fields
                // don't report a change until the user changes focus)
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        property.stringValue = EditorGUI.DelayedTextField(position, label, property.stringValue);
                        break;
                    case SerializedPropertyType.Float:
                        property.floatValue = EditorGUI.DelayedFloatField(position, label, property.floatValue);
                        break;
                    case SerializedPropertyType.Integer:
                        property.floatValue = EditorGUI.DelayedIntField(position, label, property.intValue);
                        break;
                    default:
                        // Just use a regular field for other kinds of
                        // properties
                        EditorGUI.PropertyField(position, property, new GUIContent(label));
                        break;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // A serialized declaration is read-only if it came from a Yarn
            // script. We don't allow editing those in this panel, because
            // the text of the Yarn script belongs to the user.
            bool propertyIsReadOnly = property.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null;

            const float leftInset = 8;

            Rect RectForFieldIndex(int index, int lineCount = 1)
            {
                float verticalOffset = EditorGUIUtility.singleLineHeight * index + EditorGUIUtility.standardVerticalSpacing * index;
                float height = EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);

                return new Rect(
                    position.x + leftInset,
                    position.y + verticalOffset,
                    position.width - leftInset,
                    height
                );
            }

            var foldoutPosition = RectForFieldIndex(0);

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            string name = nameProperty.stringValue;
            if (string.IsNullOrEmpty(name))
            {
                name = "Variable";
            }

            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, name);

            if (property.isExpanded)
            {
                var namePosition = RectForFieldIndex(1);
                var typePosition = RectForFieldIndex(2);
                var defaultValuePosition = RectForFieldIndex(3);
                var descriptionPosition = RectForFieldIndex(4, 2);
                var sourcePosition = RectForFieldIndex(6);

                DrawPropertyField(namePosition, nameProperty, propertyIsReadOnly);

                SerializedProperty typeProperty = property.FindPropertyRelative("type");

                DrawPropertyField(typePosition, typeProperty, propertyIsReadOnly);

                SerializedProperty defaultValueProperty;

                switch ((Yarn.Type)typeProperty.enumValueIndex)
                {
                    case Yarn.Type.Number:
                        defaultValueProperty = property.FindPropertyRelative("defaultValueNumber");
                        break;
                    case Yarn.Type.String:
                        defaultValueProperty = property.FindPropertyRelative("defaultValueString");
                        break;
                    case Yarn.Type.Bool:
                        defaultValueProperty = property.FindPropertyRelative("defaultValueBool");
                        break;
                    default:
                        defaultValueProperty = null;
                        break;
                }


                if (defaultValueProperty == null)
                {
                    EditorGUI.LabelField(defaultValuePosition, "Default Value", $"Variable type {(Yarn.Type)typeProperty.enumValueIndex} is not allowed");
                }
                else
                {
                    DrawPropertyField(defaultValuePosition, defaultValueProperty, propertyIsReadOnly, "Default Value");
                }


                // Don't use DrawPropertyField here because we want to use
                // a special gui style and directly use the string value
                SerializedProperty descriptionProperty = property.FindPropertyRelative("description");
                if (propertyIsReadOnly)
                {
                    descriptionPosition = EditorGUI.PrefixLabel(descriptionPosition, new GUIContent(descriptionProperty.displayName));
                    EditorGUI.SelectableLabel(descriptionPosition, descriptionProperty.stringValue, EditorStyles.wordWrappedLabel);
                }
                else
                {
                    var wordWrappedTextField = EditorStyles.textField;
                    wordWrappedTextField.wordWrap = true;

                    descriptionProperty.stringValue = EditorGUI.DelayedTextField(descriptionPosition, descriptionProperty.displayName, descriptionProperty.stringValue, wordWrappedTextField);
                }

                if (!propertyIsReadOnly)
                {
                    EditorGUI.LabelField(sourcePosition, "Declared In", "this file");
                }
                else
                {
                    SerializedProperty sourceProperty = property.FindPropertyRelative("sourceYarnAsset");
                    EditorGUI.ObjectField(sourcePosition, "Declared In", sourceProperty.objectReferenceValue, typeof(TextAsset), false);
                }


            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            return GetPropertyHeightImpl(property, label);
        }

        public static float GetPropertyHeightImpl(SerializedProperty property, GUIContent label)
        {
            int lines;

            if (property.isExpanded)
            {
                lines = 7;
            }
            else
            {
                lines = 1;
            }

            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * lines + 1;
        }
    }

    // A simple class lets us use a delegate as an IEqualityComparer from
    // https://stackoverflow.com/a/4607559
    internal static class Compare
    {
        public static IEqualityComparer<T> By<T>(System.Func<T, T, bool> comparison)
        {
            return new DelegateComparer<T>(comparison);
        }

        private class DelegateComparer<T> : EqualityComparer<T>
        {
            private readonly System.Func<T, T, bool> comparison;

            public DelegateComparer(System.Func<T, T, bool> identitySelector)
            {
                this.comparison = identitySelector;
            }

            public override bool Equals(T x, T y)
            {
                return comparison(x, y);
            }

            public override int GetHashCode(T obj)
            {
                // Force LINQ to never refer to the hash of an object by
                // returning a constant for all values. This is inefficient
                // because LINQ can't use an internal comparator, but we're
                // already looking to use a delegate to do a more
                // fine-grained test anyway, so we want to ensure that it's
                // called.
                return 0;
            }
        }
    }
}
