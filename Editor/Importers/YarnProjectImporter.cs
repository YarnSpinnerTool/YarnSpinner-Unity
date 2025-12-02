/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Utility;

#if USE_UNITY_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

#nullable enable

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Imports a .yarnproject file and produces a <see cref="YarnProject"/>
    /// asset.
    /// </summary>
    [ScriptedImporter(7, new[] { "yarnproject" }, 1000), HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/importing-yarn-files/yarn-projects")]
    [InitializeOnLoad]
    public class YarnProjectImporter : ScriptedImporter
    {
        /// <summary>
        /// A regular expression that matches characters following the start of
        /// the string or an underscore. 
        /// </summary>
        /// <remarks>
        /// Used as part of converting variable names from snake_case to
        /// CamelCase when generating C# variable source code.
        /// </remarks>
        private static readonly System.Text.RegularExpressions.Regex SnakeCaseToCamelCase = new System.Text.RegularExpressions.Regex(@"(^|_)(\w)");

        /// <summary>
        /// Stores information about a variable declaration found in a compiled
        /// Yarn Project.
        /// </summary>
        [System.Serializable]
        public class SerializedDeclaration
        {
            internal static List<Yarn.IType> BuiltInTypesList = new List<Yarn.IType> {
                Yarn.Types.String,
                Yarn.Types.Boolean,
                Yarn.Types.Number,
            };

            /// <summary>
            /// The name of the variable.
            /// </summary>
            public string name = "$variable";

            /// <summary>
            /// The type of the variable.
            /// </summary>
            [UnityEngine.Serialization.FormerlySerializedAs("type")]
            public string typeName = Yarn.Types.String.Name;

            /// <summary>
            /// The description of the variable.
            /// </summary>
            public string? description;

            /// <summary>
            /// Whether the variable was explicitly declared (i.e. using a
            /// <c>&lt;&lt;declare&gt;&gt;</c> statement), or whether it was
            /// implicitly declared through usage.
            /// </summary>
            public bool isImplicit;

            /// <summary>
            /// A reference to the source <c>.yarn</c> file in which the
            /// variable was declared (either implicitly or explicitly.)
            /// </summary>
            public TextAsset sourceYarnAsset;

            /// <summary>
            /// Initialises a new instance of the SerializedDeclaration class
            /// using a <see cref="Declaration"/>.
            /// </summary>
            /// <param name="decl">A <see cref="Declaration"/> containing
            /// information about a Yarn variable.</param>
            public SerializedDeclaration(Declaration decl)
            {
                this.name = decl.Name;
                this.typeName = decl.Type.Name;
                this.description = decl.Description;
                this.isImplicit = decl.IsImplicit;

                string sourceScriptPath = GetRelativePath(decl.SourceFileName);

                sourceYarnAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(sourceScriptPath);
            }
        }

        private class FunctionDeclarationReceiver : IActionRegistration
        {
            public List<Declaration> FunctionDeclarations = new();

            public void AddCommandHandler(string commandName, System.Delegate handler) { }

            public void AddCommandHandler(string commandName, MethodInfo methodInfo) { }

            public void AddFunction(string name, System.Delegate implementation) { }

            public void RegisterFunctionDeclaration(string name, System.Type returnType, System.Type[] parameterTypes)
            {
                if (Types.TypeMappings.TryGetValue(returnType, out var returnYarnType) == false)
                {
                    Debug.LogError($"Can't register function {name}: can't convert return type {returnType} to a Yarn type");
                    return;
                }

                var typeBuilder = new FunctionTypeBuilder().WithReturnType(returnYarnType);


                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    System.Type? parameter = parameterTypes[i];

                    bool isParamsArray = false;

                    if (i == parameterTypes.Length - 1 && parameter.IsArray)
                    {
                        // If this is the last parameter and it is an array,
                        // treat it as though it were a params array and use the
                        // type of the array
                        parameter = parameter.GetElementType();
                        isParamsArray = true;
                    }

                    if (Types.TypeMappings.TryGetValue(parameter, out var parameterYarnType) == false)
                    {
                        Debug.LogError($"Can't register function {name}: can't convert parameter {i} type {parameterYarnType} to a Yarn type");
                        return;
                    }

                    if (isParamsArray)
                    {
                        typeBuilder = typeBuilder.WithVariadicParameterType(parameterYarnType);
                    }
                    else
                    {
                        typeBuilder = typeBuilder.WithParameter(parameterYarnType);
                    }
                }

                var decl = new DeclarationBuilder()
                    .WithName(name)
                    .WithType(typeBuilder.FunctionType)
                    .Declaration;

                this.FunctionDeclarations.Add(decl);
            }

            public void RemoveCommandHandler(string commandName) { }

            public void RemoveFunction(string name) { }
        }

        /// <summary>
        /// Whether to generate a C# file that contains properties for each variable.
        /// </summary>
        /// <seealso cref="variablesClassName"/>
        /// <seealso cref="variablesClassNamespace"/>
        /// <seealso cref="variablesClassParent"/>
        public bool generateVariablesSourceFile = false;

        /// <summary>
        /// The name of the generated variables storage class.
        /// </summary>
        /// <seealso cref="generateVariablesSourceFile"/>
        /// <seealso cref="variablesClassNamespace"/>
        /// <seealso cref="variablesClassParent"/>
        public string variablesClassName = "YarnVariables";

        /// <summary>
        /// The namespace of the generated variables storage class.
        /// </summary>
        /// <seealso cref="generateVariablesSourceFile"/>
        /// <seealso cref="variablesClassName"/>
        /// <seealso cref="variablesClassParent"/>
        public string? variablesClassNamespace = null;

        /// <summary>
        /// The parent class of the generated variables storage class.
        /// </summary>
        /// <seealso cref="generateVariablesSourceFile"/>
        /// <seealso cref="variablesClassName"/>
        /// <seealso cref="variablesClassNamespace"/>
        public string variablesClassParent = typeof(InMemoryVariableStorage).FullName;

        /// <summary>
        /// Whether or not this Yarn project's built-in Localizations will use
        /// Addressable Assets.
        /// </summary>
        /// <remarks>This value is only used when the project is not configured
        /// to use Unity Localization.</remarks>
        public bool useAddressableAssets;

        internal static string UnityProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // Scripted importers can't have direct references to scriptable
        // objects, so we'll store the reference as a string containing the
        // GUID. This is also used to store a reference to a string table if
        // Unity Localisation is not installed.
        public string? unityLocalisationStringTableCollectionGUID;

        public bool UseUnityLocalisationSystem = false;
#if USE_UNITY_LOCALIZATION

        private StringTableCollection? _cachedStringTableCollection;

        /// <summary>
        /// Gets or sets the Unity Localization string table collection
        /// associated with this importer.
        /// </summary>
        public StringTableCollection? UnityLocalisationStringTableCollection
        {
            get
            {
                if (_cachedStringTableCollection == null)
                {
                    if (!string.IsNullOrEmpty(unityLocalisationStringTableCollectionGUID))
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(unityLocalisationStringTableCollectionGUID);

                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            _cachedStringTableCollection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(assetPath);
                        }
                    }
                }
                return _cachedStringTableCollection;
            }
            set
            {
                if (value == null)
                {
                    unityLocalisationStringTableCollectionGUID = string.Empty;
                    _cachedStringTableCollection = null;
                    return;
                }

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out var guid, out long _))
                {
                    throw new System.InvalidOperationException($"String table collection {value.name} has no GUID - is it not an asset stored on disk?");
                }

                unityLocalisationStringTableCollectionGUID = guid;
                _cachedStringTableCollection = value;
            }
        }
#endif

        /// <summary>
        /// Gets a <see cref="Project"/> loaded from this importer's asset file,
        /// or <see langword="null"/> if an error is encountered.
        /// </summary>
        /// <returns>A loaded <see cref="Project"/> representing the data from
        /// the file that this asset importer represents, or <see
        /// langword="null"/>.</returns>
        public Project? GetProject()
        {
            try
            {
                return Project.LoadFromFile(this.assetPath);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="ProjectImportData"/> created the last time that
        /// this Yarn Project was imported, if available.
        /// </summary>
        public ProjectImportData? ImportData => AssetDatabase.LoadAssetAtPath<ProjectImportData>(this.assetPath);

        /// <summary>
        /// Gets a value indicating whether this Yarn Project includes a Yarn
        /// script as part of its compilation.
        /// </summary>
        /// <param name="yarnImporter">The importer for a Yarn script.</param>
        /// <returns><see langword="true"/> if this Yarn Project uses the file
        /// represented by yarnImporter; <see langword="false"/>
        /// otherwise.</returns>
        public bool GetProjectReferencesYarnFile(YarnImporter yarnImporter)
        {
            try
            {
                var project = Project.LoadFromFile(this.assetPath);
                var scriptFile = yarnImporter.assetPath;

                var scriptFileWithEnvironmentSeparators = string.Join(System.IO.Path.DirectorySeparatorChar, scriptFile.Split('/'));

                var projectRelativeSourceFiles = project.SourceFiles.Select(GetRelativePath);

                return projectRelativeSourceFiles.Contains(scriptFileWithEnvironmentSeparators);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the Yarn string table produced as a result of compiling the Yarn
        /// Project, or <see langword="null"/> if no string table could be produced.
        /// </summary>
        private Dictionary<string, StringInfo>? GetYarnStringTable()
        {
            Project project;
            try
            {
                project = Project.LoadFromFile(this.assetPath);
            }
            catch (System.Exception)
            {
                return null;
            }

            var job = CompilationJob.CreateFromFiles(project.SourceFiles);
            job.LanguageVersion = project.FileVersion;
            job.CompilationType = CompilationJob.Type.StringsOnly;

            try
            {
                var compilationResult = Compiler.Compiler.Compile(job);

                return new(compilationResult.StringTable);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Called by Unity to import an asset.
        /// </summary>
        /// <param name="ctx">The context for the asset import
        /// operation.</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
#if YARNSPINNER_DEBUG
            UnityEngine.Profiling.Profiler.enabled = true;
#endif

            var projectAsset = ScriptableObject.CreateInstance<YarnProject>();

            projectAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

            // Start by creating the asset - no matter what, we need to
            // produce an asset, even if it doesn't contain valid Yarn
            // bytecode, so that other assets don't lose their references.
            ctx.AddObjectToAsset("Project", projectAsset);
            ctx.SetMainObject(projectAsset);

            var importData = ScriptableObject.CreateInstance<ProjectImportData>();
            importData.name = "Project Import Data";
            ctx.AddObjectToAsset("ImportData", importData);

            // Attempt to load the JSON project file.
            Project project;
            try
            {
                project = Yarn.Compiler.Project.LoadFromFile(ctx.assetPath);
            }
            catch (System.Exception)
            {
                var text = File.ReadAllText(ctx.assetPath);
                if (text.StartsWith("title:"))
                {
                    // This is an old-style project that needs to be upgraded.
                    importData.ImportStatus = ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1;

                    // Log to notify the user that this needs to be done.
                    ctx.LogImportError($"Yarn Project {ctx.assetPath} is a version 1 Yarn Project, and needs to be upgraded. Select it in the Inspector, and click Upgrade Yarn project.", this);
                }
                else
                {
                    // We don't know what's going on.
                    importData.ImportStatus = ProjectImportData.ImportStatusCode.Unknown;
                }
                // Either way, we can't continue.
                return;
            }

            importData.sourceFilePatterns = new();
            importData.sourceFilePatterns.AddRange(project.SourceFilePatterns);

            importData.baseLanguageName = project.BaseLanguage;

            foreach (var loc in project.Localisation)
            {
                ProjectImportData.LocalizationEntry locInfo;

                // am force unwrapping the strings due to a bug
                // the IsNullOrEmpty check on this version of dotnet doesn't propogate it's understanding that the value isn't null
                // in a future version this will go away as a concern.
                if (string.IsNullOrEmpty(loc.Value.Strings) == false && loc.Value.Strings!.StartsWith("unity:"))
                {
                    // This is an external Localization asset.
                    locInfo = new ProjectImportData.LocalizationEntry
                    {
                        languageID = loc.Key,
                        isExternal = true,
                        externalLocalization = AssetDatabase.LoadAssetAtPath<Localization>(AssetDatabase.GUIDToAssetPath(loc.Value.Strings.Substring("unity:".Length)))
                    };
                }
                else
                {
                    var hasStringsFile = project.TryGetStringsPath(loc.Key, out var stringsFilePath);
                    var hasAssetsFolder = project.TryGetAssetsPath(loc.Key, out var assetsFolderPath);

                    // This is a reference to a strings table file and a folder
                    // containing assets.
                    locInfo = new ProjectImportData.LocalizationEntry
                    {
                        languageID = loc.Key,
                        isExternal = false,
                        stringsFile = hasStringsFile ? AssetDatabase.LoadAssetAtPath<TextAsset>(stringsFilePath) : null,
                        assetsFolder = hasAssetsFolder ? AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetsFolderPath) : null
                    };
                }
                importData.localizations.Add(locInfo);
            }

            if (project.Localisation.ContainsKey(project.BaseLanguage) == false)
            {
                importData.localizations.Add(new ProjectImportData.LocalizationEntry
                {
                    languageID = project.BaseLanguage,
                });
            }

            var projectRelativeSourceFiles = project.SourceFiles.Select(GetRelativePath);

            CompilationResult compilationResult;

            if (projectRelativeSourceFiles.Any())
            {
                // This project depends upon this script
                foreach (var scriptPath in projectRelativeSourceFiles)
                {
                    string guid = AssetDatabase.AssetPathToGUID(scriptPath);

                    ctx.DependsOnSourceAsset(scriptPath);

                    importData.yarnFiles.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(scriptPath));
                }

                // Get all function declarations found in the Unity project
                var functionDeclarationReceiver = new FunctionDeclarationReceiver();

                foreach (var registrationAction in Actions.ActionRegistrationMethods)
                {
                    registrationAction(functionDeclarationReceiver, RegistrationType.Compilation);
                }

                // Now to compile the scripts associated with this project.
                var job = CompilationJob.CreateFromFiles(project.SourceFiles);
                job.LanguageVersion = project.FileVersion;
                job.Declarations = functionDeclarationReceiver.FunctionDeclarations;

                try
                {
                    compilationResult = Compiler.Compiler.Compile(job);
                }
                catch (System.Exception e)
                {
                    var errorMessage = $"Encountered an unhandled exception during compilation: {e.Message}";
                    Debug.LogException(e);
                    ctx.LogImportError(errorMessage, null);

                    importData.diagnostics.Add(new ProjectImportData.DiagnosticEntry
                    {
                        yarnFile = null,
                        errorMessages = new List<string> { errorMessage },
                    });
                    importData.ImportStatus = ProjectImportData.ImportStatusCode.CompilationFailed;
                    return;
                }

                var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

                if (errors.Count() > 0)
                {
                    var errorGroups = errors.GroupBy(e => e.FileName);
                    foreach (var errorGroup in errorGroups)
                    {
                        if (errorGroup.Key == null)
                        {
                            // ok so we have no file for some reason
                            // so these are errors currently not tied to a file
                            // so we instead need to just log the errors and move on
                            foreach (var error in errorGroup)
                            {
                                ctx.LogImportError($"Error compiling project: {error.Message}");
                            }

                            importData.diagnostics.Add(new ProjectImportData.DiagnosticEntry
                            {
                                yarnFile = null,
                                errorMessages = errorGroup.Select(e => e.Message).ToList(),
                            });

                            continue;
                        }

                        // the compiler currently returns (unknown) for situations where an error is defined in a file that the compiler can't access
                        // this is not ideal and something to fix, but for now we will handle it by reporting the issue in a different way
                        if (errorGroup.Key == "(unknown)")
                        {
                            foreach (var error in errorGroup)
                            {
                                ctx.LogImportError($"Error compiling the project: {error.Message}");
                            }
                            var errorMessages = errorGroup.Select(e => e.ToString());
                            importData.diagnostics.Add(new ProjectImportData.DiagnosticEntry
                            {
                                yarnFile = null,
                                errorMessages = errorMessages.ToList(),
                            });
                            continue;
                        }

                        try
                        {
                            var relativePath = GetRelativePath(errorGroup.Key);

                            var asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);

                            foreach (var error in errorGroup)
                            {
                                var relativeErrorFileName = GetRelativePath(error.FileName);
                                ctx.LogImportError($"Error compiling <a href=\"{relativeErrorFileName}\">{relativeErrorFileName}</a> line {error.Range.Start.Line + 1}: {error.Message}", asset);
                            }

                            var fileWithErrors = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);

                            // TODO: Associate this compile error to the
                            // corresponding script

                            var errorMessages = errorGroup.Select(e => e.ToString());
                            importData.diagnostics.Add(new ProjectImportData.DiagnosticEntry
                            {
                                yarnFile = fileWithErrors,
                                errorMessages = errorMessages.ToList(),
                            });
                        }
                        catch (System.Exception ex)
                        {
                            ctx.LogImportError($"Import failed with an unhandled exception: {ex.Message}");
                        }
                    }
                    importData.ImportStatus = ProjectImportData.ImportStatusCode.CompilationFailed;
                    return;
                }

                if (compilationResult.Program == null)
                {
                    ctx.LogImportError("Internal error: Failed to compile: resulting program was null, but compiler did not report errors.");
                    return;
                }

                importData.containsImplicitLineIDs = compilationResult.ContainsImplicitStringTags;

                // Store _all_ declarations - both the ones in this .yarnproject
                // file, and the ones inside the .yarn files.

                // While we're here, filter out any declarations that begin with
                // our Yarn internal prefix. These are synthesized variables
                // that are generated as a result of the compilation, and are
                // not declared by the user.
                importData.serializedDeclarations = compilationResult.Declarations
                    .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
                    .Where(decl => !(decl.Type is FunctionType))
                    .Select(decl => new SerializedDeclaration(decl)).ToList();

#if USE_UNITY_LOCALIZATION
                if (UseUnityLocalisationSystem)
                {
                    // Mark that this project uses Unity Localization; we'll
                    // populate the string table later, in a post-processor.
                    projectAsset.localizationType = LocalizationType.Unity;
                }
                else
                {
                    CreateYarnInternalLocalizationAssets(ctx, projectAsset, compilationResult, importData);
                    projectAsset.localizationType = LocalizationType.YarnInternal;
                }
#else
                CreateYarnInternalLocalizationAssets(ctx, projectAsset, compilationResult, importData);
                projectAsset.localizationType = LocalizationType.YarnInternal;
#endif

                // Store the compiled program
                byte[] compiledBytes;

                using (var memoryStream = new MemoryStream())
                using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
                {
                    // Serialize the compiled program to memory
                    compilationResult.Program.WriteTo(outputStream);
                    outputStream.Flush();

                    compiledBytes = memoryStream.ToArray();
                }

                projectAsset.compiledYarnProgram = compiledBytes;

                if (generateVariablesSourceFile)
                {

                    var fileName = variablesClassName + ".cs";

                    var generatedSourcePath = Path.Combine(Path.GetDirectoryName(ctx.assetPath), fileName);
                    bool generated = GenerateVariableSource(generatedSourcePath, project, compilationResult);
                    if (generated)
                    {
                        AssetDatabase.ImportAsset(generatedSourcePath);
                    }
                }
            }

            importData.ImportStatus = ProjectImportData.ImportStatusCode.Succeeded;

#if YARNSPINNER_DEBUG
            UnityEngine.Profiling.Profiler.enabled = false;
#endif
        }

        private bool GenerateVariableSource(string outputPath, Project project, CompilationResult compilationResult)
        {
            string? existingContent = null;

            if (File.Exists(outputPath))
            {
                // If the file already exists on disk, read it all in now. We'll
                // compare it to what we generated and, if the contents match
                // exactly, we don't need to re-import the resulting C# script.
                existingContent = File.ReadAllText(outputPath);
            }

            if (string.IsNullOrEmpty(variablesClassName))
            {
                Debug.LogError("Can't generate variable interface, because the specified class name is empty.");
                return false;
            }

            StringBuilder sb = new StringBuilder();
            int indentLevel = 0;
            const int indentSize = 4;

            void WriteLine(string line = "", int offset = 0)
            {
                if (line.Length > 0)
                {
                    sb.Append(new string(' ', (indentLevel + offset) * indentSize));
                }
                sb.AppendLine(line);
            }
            void WriteComment(string comment = "") => WriteLine("// " + comment);

            if (string.IsNullOrEmpty(variablesClassNamespace) == false)
            {
                WriteLine($"namespace {variablesClassNamespace} {{");
                WriteLine();
                indentLevel += 1;
            }

            WriteLine("using Yarn.Unity;");
            WriteLine();

            void WriteGeneratedCodeAttribute()
            {
                var toolName = "YarnSpinner";
                var toolVersion = this.GetType().Assembly.GetName().Version.ToString();
                WriteLine($"[System.CodeDom.Compiler.GeneratedCode(\"{toolName}\", \"{toolVersion}\")]");
            }

            // For each user-defined enum, create a C# enum type
            IEnumerable<EnumType> enumTypes = compilationResult.UserDefinedTypes.OfType<Yarn.EnumType>();

            foreach (var type in enumTypes)
            {
                WriteLine($"/// <summary>");
                if (string.IsNullOrEmpty(type.Description) == false)
                {
                    WriteLine($"/// {type.Description}");
                }
                else
                {
                    WriteLine($"/// {type.Name}");
                }
                WriteLine($"/// </summary>");

                WriteLine($"/// <remarks>");
                WriteLine($"/// Automatically generated from Yarn project at {this.assetPath}.");
                WriteLine($"/// </remarks>");

                WriteGeneratedCodeAttribute();

                // Enums are always stored as integers; strings are represented
                // as CRC32 hashes of the raw value
                WriteLine($"public enum {type.Name} {{");

                indentLevel += 1;

                foreach (var enumCase in type.EnumCases)
                {
                    WriteLine();

                    WriteLine($"/// <summary>");
                    if (string.IsNullOrEmpty(enumCase.Value.Description) == false)
                    {
                        WriteLine($"/// {enumCase.Value.Description}");
                    }
                    else
                    {
                        WriteLine($"/// {enumCase.Key}");
                    }
                    WriteLine($"/// </summary>");

                    if (type.RawType == Types.Number)
                    {
                        WriteLine($"{enumCase.Key} = {enumCase.Value.Value},");
                    }
                    else if (type.RawType == Types.String)
                    {
                        WriteLine($"/// <remarks>");
                        WriteLine($"/// Backing value: \"{enumCase.Value.Value}\"");
                        WriteLine($"/// </remarks>");
                        var stringValue = (string)enumCase.Value.Value;
                        WriteComment($"\"{stringValue}\"");
                        // Get the hash of the string, and convert it to a
                        // signed integer. (Unity doesn't correctly handle enums
                        // whose backing value is a uint (values over signed
                        // integer max are clamped to zero), so we'll cast it here.)
                        WriteLine($"{enumCase.Key} = {(int)CRC32.GetChecksum(stringValue)},");
                    }
                    else
                    {
                        WriteComment($"Error: enum case {type.Name}.{enumCase.Key} has an invalid raw type {type.RawType.Name}");
                    }
                }

                indentLevel -= 1;

                WriteLine($"}}");
                WriteLine();
            }

            if (enumTypes.Any())
            {
                // Generate an extension class that extends the above enums with
                // methods that accesses their backing value
                WriteGeneratedCodeAttribute();
                WriteLine($"internal static class {variablesClassName}TypeExtensions {{");
                indentLevel += 1;
                foreach (var enumType in enumTypes)
                {
                    var backingType = enumType.RawType == Types.Number ? "int" : "string";
                    WriteLine($"internal static {backingType} GetBackingValue(this {enumType.Name} enumValue) {{");
                    indentLevel += 1;
                    WriteLine($"switch (enumValue) {{");
                    indentLevel += 1;

                    foreach (var @case in enumType.EnumCases)
                    {
                        WriteLine($"case {enumType.Name}.{@case.Key}:", 1);
                        if (enumType.RawType == Types.Number)
                        {

                            WriteLine($"return {@case.Value.Value};", 2);
                        }
                        else if (enumType.RawType == Types.String)
                        {
                            WriteLine($"return \"{@case.Value.Value}\";", 2);
                        }
                        else
                        {
                            throw new System.ArgumentException($"Invalid Yarn enum raw type {enumType.RawType}");
                        }
                    }
                    WriteLine("default:", 1);
                    WriteLine("throw new System.ArgumentException($\"{enumValue} is not a valid enum case.\");");

                    indentLevel -= 1;
                    WriteLine("}");
                    indentLevel -= 1;
                    WriteLine("}");
                }
                indentLevel -= 1;
                WriteLine("}");
            }

            WriteGeneratedCodeAttribute();
            WriteLine($"public partial class {variablesClassName} : {variablesClassParent}, Yarn.Unity.IGeneratedVariableStorage {{");

            indentLevel += 1;

            var declarationsToGenerate = compilationResult.Declarations
                .Where(d => d.IsVariable == true)
                .Where(d => d.Name.StartsWith("$Yarn.Internal") == false);

            if (declarationsToGenerate.Count() == 0)
            {
                WriteComment("This yarn project does not declare any variables.");
            }

            foreach (var decl in declarationsToGenerate)
            {
                string? cSharpTypeName = null;

                if (decl.Type == Yarn.Types.String)
                {
                    cSharpTypeName = "string";
                }
                else if (decl.Type == Yarn.Types.Number)
                {
                    cSharpTypeName = "float";
                }
                else if (decl.Type == Yarn.Types.Boolean)
                {
                    cSharpTypeName = "bool";
                }
                else if (decl.Type is EnumType enumType1)
                {
                    cSharpTypeName = enumType1.Name;
                }
                else
                {
                    WriteLine($"#warning Can't generate a property for variable {decl.Name}, because its type ({decl.Type}) can't be handled.");
                    WriteLine();
                }


                WriteComment($"Accessor for {decl.Type} {decl.Name}");

                // Remove '$'
                string cSharpVariableName = decl.Name.TrimStart('$');

                // Convert snake_case to CamelCase
                cSharpVariableName = SnakeCaseToCamelCase.Replace(cSharpVariableName, (match) =>
                {
                    return match.Groups[2].Value.ToUpperInvariant();
                });

                // Capitalise first letter
                cSharpVariableName = cSharpVariableName.Substring(0, 1).ToUpperInvariant() + cSharpVariableName.Substring(1);

                if (decl.Description != null)
                {
                    WriteLine("/// <summary>");
                    WriteLine($"/// {decl.Description}");
                    WriteLine("/// </summary>");
                }

                WriteLine($"public {cSharpTypeName} {cSharpVariableName} {{");

                indentLevel += 1;

                if (decl.Type is EnumType enumType)
                {
                    WriteLine($"get => this.GetEnumValueOrDefault<{cSharpTypeName}>(\"{decl.Name}\");");
                }
                else
                {
                    WriteLine($"get => this.GetValueOrDefault<{cSharpTypeName}>(\"{decl.Name}\");");
                }

                if (decl.IsInlineExpansion == false)
                {
                    // Only generate a setter if it's a variable that can be modified
                    if (decl.Type is EnumType e)
                    {
                        WriteLine($"set => this.SetValue(\"{decl.Name}\", value.GetBackingValue());");
                    }
                    else
                    {
                        WriteLine($"set => this.SetValue<{cSharpTypeName}>(\"{decl.Name}\", value);");
                    }
                }
                indentLevel -= 1;

                WriteLine($"}}");

                WriteLine();
            }

            indentLevel -= 1;

            WriteLine($"}}");

            if (string.IsNullOrEmpty(variablesClassNamespace) == false)
            {
                indentLevel -= 1;
                WriteLine($"}}");
            }

            if (existingContent != null && existingContent.Equals(sb.ToString(), System.StringComparison.Ordinal))
            {
                // What we generated is identical to what's already on disk.
                // Don't write it.
                return false;
            }

            Debug.Log($"Writing to {outputPath}");
            File.WriteAllText(outputPath, sb.ToString());

            return true;

        }

        /// <summary>
        /// Checks if the modifications on the Asset Database will necessitate a
        /// reimport of the project to stay in sync with the localisation
        /// assets.
        /// </summary>
        /// <remarks>
        /// Because assets can be added and removed after associating a folder
        /// of assets with a locale, modifications won't be detected until
        /// runtime when they cause an error. This is bad for many reasons, so
        /// this method will check any modified assets and see if they
        /// correspond to this Yarn Project. If they do, it will reimport the
        /// project to reassociate them.
        /// </remarks>
        /// <param name="modifiedAssetPaths">The list of asset paths that have
        /// been modified; that is to say, assets that have been added, removed,
        /// or moved.</param>
        public void CheckUpdatedAssetsRequireReimport(List<string> modifiedAssetPaths)
        {
            // Use an inner method that can return early if it detects that an
            // asset has been modified.
            bool IsAnyAssetModified()
            {
                if (ImportData == null)
                {
                    // We don't have any information we can use to determine whether
                    // we need to re-import or not. Assume that we need to.
                    return true;
                }
                var localeAssetFolderPaths = ImportData.localizations.Where(l => l.assetsFolder != null).Select(l => AssetDatabase.GetAssetPath(l.assetsFolder));

                var comparison = System.StringComparison.CurrentCulture;
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    comparison = System.StringComparison.OrdinalIgnoreCase;
                }
                foreach (var path in localeAssetFolderPaths)
                {
                    // we need to ensure we have the trailing seperator otherwise it is to be considered a file
                    // and files can never be the parent of another file
                    var assetPath = path;
                    if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        assetPath += Path.DirectorySeparatorChar.ToString();
                    }

                    foreach (var modified in modifiedAssetPaths)
                    {
                        if (modified.StartsWith(assetPath, comparison))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            if (IsAnyAssetModified())
            {
                AssetDatabase.ImportAsset(this.assetPath);
            }
        }

        internal static string GetRelativePath(string path)
        {
            if (path.StartsWith(UnityProjectRootPath) == false)
            {
                // This is not a child of the current project. If it's an
                // absolute path, then it's enough to go on.
                if (Path.IsPathRooted(path))
                {
                    return path;
                }
                else
                {
                    throw new System.ArgumentException($"Path {path} is not a child of the project root path {UnityProjectRootPath}");
                }
            }
            // Trim the root path off along with the trailing slash
            return path.Substring(UnityProjectRootPath.Length + 1);
        }

        private void CreateYarnInternalLocalizationAssets(AssetImportContext ctx, YarnProject projectAsset, CompilationResult compilationResult, ProjectImportData importData)
        {
            // Will we need to create a default localization? This variable
            // will be set to false if any of the languages we've
            // configured in languagesToSourceAssets is the default
            // language.
            var shouldAddDefaultLocalization = true;

            foreach (var localisationInfo in importData.localizations)
            {
                if (localisationInfo.isExternal)
                {
                    // Don't need to create a localization asset because an
                    // external asset was provided
                    continue;
                }

                // Don't create a localization if the language ID was not
                // provided
                if (string.IsNullOrEmpty(localisationInfo.languageID))
                {
                    Debug.LogWarning($"Not creating a localization for {projectAsset.name} because the language ID wasn't provided.");
                    continue;
                }

                IEnumerable<StringTableEntry>? stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (localisationInfo.languageID == importData.baseLanguageName)
                {
                    // No strings file needed - we'll use the program-supplied string table.
                    stringTable = GenerateStringsTable(compilationResult);

                    // We don't need to add a default localization.
                    shouldAddDefaultLocalization = false;
                }
                else
                {
                    // No strings file provided
                    if (localisationInfo.stringsFile == null)
                    {
                        Debug.LogWarning($"Not creating a localisation for {localisationInfo.languageID} in the Yarn project {projectAsset.name} because a strings file was not specified, and {localisationInfo.languageID} is not the project's base language");
                        continue;
                    }
                    try
                    {
                        stringTable = StringTableEntry.ParseFromCSV(localisationInfo.stringsFile.text);
                    }
                    catch (System.ArgumentException e)
                    {
                        Debug.LogWarning($"Not creating a localization for {localisationInfo.languageID} in the Yarn Project {projectAsset.name} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                }

                var newLocalization = ScriptableObject.CreateInstance<Localization>();

                if (stringTable != null)
                {
                    // Add these new lines to the localisation's asset
                    foreach (var entry in stringTable)
                    {
                        newLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text ?? string.Empty);
                    }
                }

                projectAsset.localizations.Add(localisationInfo.languageID, newLocalization);
                newLocalization.name = localisationInfo.languageID;

                if (localisationInfo.assetsFolder != null)
                {
#if USE_ADDRESSABLES
                    const bool addressablesAvailable = true;
#else
                    const bool addressablesAvailable = false;
#endif

                    if (addressablesAvailable && useAddressableAssets)
                    {
                        newLocalization.UsesAddressableAssets = true;
                    }

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
                    var stringIDsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, AssetDatabase.GetAssetPath(localisationInfo.assetsFolder), typeof(UnityEngine.Object));

                    // Load the asset, so we can assign the reference.
                    var assetPaths = stringIDsToAssetPaths
                        .Select(a => new KeyValuePair<string, Object>(a.Key, AssetDatabase.LoadAssetAtPath<Object>(a.Value)));

                    foreach (var (id, asset) in assetPaths)
                    {
                        newLocalization.AddLocalizedObjectToAsset(id, asset);
#if USE_ADDRESSABLES
                        if (newLocalization.UsesAddressableAssets)
                        {
                            // If we're using addressable assets, make sure that
                            // the asset we just added has an address
                            LocalizationEditor.EnsureAssetIsAddressable(asset, Localization.GetAddressForLine(id, localisationInfo.languageID));
                        }
#endif
                    }

#if YARNSPINNER_DEBUG
                    stopwatch.Stop();
                    Debug.Log($"Imported {stringIDsToAssetPaths.Count()} assets for {project.name} \"{pair.languageID}\" in {stopwatch.ElapsedMilliseconds}ms");
#endif
                }

                ctx.AddObjectToAsset("localization-" + localisationInfo.languageID, newLocalization);

                if (localisationInfo.languageID == importData.baseLanguageName)
                {
                    // If this is our default language, set it as such
                    projectAsset.baseLocalization = newLocalization;

                    // Since this is the default language, also populate the line metadata.
                    projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
                }
                else if (localisationInfo.stringsFile != null)
                {
                    // This localization depends upon a source asset. Make
                    // this asset get re-imported if this source asset was
                    // modified
                    ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(localisationInfo.stringsFile));
                }
            }

            if (shouldAddDefaultLocalization)
            {
                // We didn't add a localization for the default language.
                // Create one for it now.
                var stringTableEntries = GetStringTableEntries(compilationResult);

                var developmentLocalization = ScriptableObject.CreateInstance<Localization>();
                developmentLocalization.name = $"Default ({importData.baseLanguageName})";


                // Add these new lines to the development localisation's asset
                foreach (var entry in stringTableEntries)
                {
                    developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text ?? string.Empty);
                }

                projectAsset.baseLocalization = developmentLocalization;
                projectAsset.localizations.Add(importData.baseLanguageName ?? developmentLocalization.name, projectAsset.baseLocalization);
                ctx.AddObjectToAsset("default-language", developmentLocalization);

                // Since this is the default language, also populate the line metadata.
                projectAsset.lineMetadata = new LineMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
            }

            foreach (var locInfo in importData.localizations.Where(l => l.isExternal && l.externalLocalization != null))
            {
                // Add external localisations to this project's list
                projectAsset.localizations.Add(locInfo.languageID, locInfo.externalLocalization!);
            }
        }

#if USE_UNITY_LOCALIZATION
        private static void AddStringTableEntries(IDictionary<string, StringInfo> stringTable, StringTableCollection unityLocalisationStringTableCollection, string baseLanguage)
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                // No localization settings available. We can't add string table entries.
                Debug.LogWarning($"Unity Localization is installed, but your project has no Localization Settings.");
                return;
            }

            // Get the Unity string table corresponding to the Yarn Project's
            // base language. If a table can't be found for the language but can
            // be for the language's parent, use that. Otherwise, return null.
            StringTable? FindBaseLanguageStringTable(string baseLanguage)
            {
                StringTable baseLanguageStringTable = unityLocalisationStringTableCollection.StringTables
                    .FirstOrDefault(t => t.LocaleIdentifier == baseLanguage);

                if (baseLanguageStringTable != null)
                {
                    return baseLanguageStringTable;
                }

                // We didn't find a string table that exactly matches the locale
                // code of our Yarn Project's base language. Maybe we can try to
                // find a string table for our base language's parent.

                System.Globalization.CultureInfo? defaultCulture = null;
                try
                {
                    defaultCulture = new System.Globalization.CultureInfo(baseLanguage);
                }
                catch (System.Globalization.CultureNotFoundException)
                {
                    // We can't find a CultureInfo for the base language.
                    return null;
                }

                if (defaultCulture.IsNeutralCulture)
                {
                    // The base language is a neutral culture. It has no parent
                    // we could look for.
                    return null;
                }

                var defaultNeutralCulture = defaultCulture.Parent;

                var defaultNeutralStringTable = unityLocalisationStringTableCollection.StringTables.FirstOrDefault(table => table.LocaleIdentifier == defaultNeutralCulture.Name);

                return defaultNeutralStringTable;
            }

            var unityStringTable = FindBaseLanguageStringTable(baseLanguage);

            if (unityStringTable == null)
            {
                Debug.LogWarning($"Unable to find a locale in the string table that matches the default locale {baseLanguage}");
                return;
            }

            foreach (var yarnEntry in stringTable)
            {
                // Grab the data that we'll put in the string table
                var lineID = yarnEntry.Key;
                var stringInfo = yarnEntry.Value;

                // Do we already have an entry with this line ID?
                UnityEngine.Localization.Tables.StringTableEntry unityEntry = unityStringTable.GetEntry(lineID);

                if (unityEntry != null)
                {
                    // We have an existing entry, so update it.
                    unityEntry.Value = stringInfo.text;
                }
                else
                {
                    // Create a new entry for this content.
                    unityEntry = unityStringTable.AddEntry(lineID, stringInfo.text);
                }

                // Next, set up the metadata on this entry. We'll start by
                // getting the list of hashtags on the line, not including its
                // line ID (we don't need it in metadata, because it's already
                // stored as the table entry's key.)
                var tags = RemoveLineIDFromMetadata(stringInfo.metadata).ToArray();

                // Next, do we already have metadata for the Unity table entry?
                var existingSharedMetadata = unityEntry.SharedEntry.Metadata.GetMetadata<UnityLocalization.LineMetadata>();

                if (existingSharedMetadata != null)
                {
                    // We do. Update the existing metadata.
                    existingSharedMetadata.nodeName = stringInfo.nodeName;
                    existingSharedMetadata.tags = tags;
                }
                else
                {
                    // Create a new metadata.
                    unityEntry.SharedEntry.Metadata.AddMetadata(new UnityLocalization.LineMetadata
                    {
                        nodeName = stringInfo.nodeName,
                        tags = tags,
                    });
                }
            }

            // We've made changes to the table, so flag it and its shared data
            // as dirty.
            EditorUtility.SetDirty(unityStringTable);
            EditorUtility.SetDirty(unityStringTable.SharedData);
            EditorUtility.SetDirty(unityLocalisationStringTableCollection);
            EditorUtility.SetDirty(LocalizationEditorSettings.ActiveLocalizationSettings);
            return;

        }
#endif

        /// <summary>
        /// Gets a value indicating whether this Yarn Project contains any
        /// compile errors.
        /// </summary>
        internal bool HasErrors
        {
            get
            {
                var importData = AssetDatabase.LoadAssetAtPath<ProjectImportData>(this.assetPath);

                if (importData == null)
                {
                    // If we have no import data, then a problem has occurred
                    // when importing this project, so indicate 'true' as
                    // signal.
                    return true;
                }
                return importData.HasCompileErrors;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this Yarn Project is able to
        /// generate a strings table - that is, it has no compile errors,
        /// it has at least one script, and all scripts are fully tagged.
        /// </summary>
        /// <inheritdoc path="exception"
        /// cref="GetScriptHasLineTags(TextAsset)"/>
        internal bool CanGenerateStringsTable
        {
            get
            {
                var importData = AssetDatabase.LoadAssetAtPath<ProjectImportData>(this.assetPath);

                if (importData == null)
                {
                    return false;
                }

                return importData.HasCompileErrors == false && importData.containsImplicitLineIDs == false;
            }
        }

        internal CompilationJob GetCompilationJob()
        {
            var project = GetProject();

            if (project == null)
            {
                return default;
            }

            return CompilationJob.CreateFromFiles(project.SourceFiles);
        }

        internal IEnumerable<string> GetErrorsForScript(TextAsset sourceScript)
        {
            if (ImportData == null)
            {
                return Enumerable.Empty<string>();
            }
            foreach (var errorCollection in ImportData.diagnostics)
            {
                if (errorCollection.yarnFile == sourceScript)
                {
                    return errorCollection.errorMessages;
                }
            }
            return Enumerable.Empty<string>();
        }

        internal IEnumerable<StringTableEntry>? GenerateStringsTable()
        {
            var job = GetCompilationJob();
            job.CompilationType = CompilationJob.Type.StringsOnly;
            var result = Compiler.Compiler.Compile(job);
            return GenerateStringsTable(result);

        }

        /// <summary>
        /// Generates a collection of <see cref="StringTableEntry"/>
        /// objects, one for each line in this Yarn Project's scripts.
        /// </summary>
        /// <returns>An IEnumerable containing a <see
        /// cref="StringTableEntry"/> for each of the lines in the Yarn
        /// Project, or <see langword="null"/> if the Yarn Project contains
        /// errors.</returns>
        internal IEnumerable<StringTableEntry>? GenerateStringsTable(CompilationResult compilationResult)
        {
            if (compilationResult == null)
            {
                // We only get no value if we have no scripts to work with.
                // In this case, return an empty collection - there's no
                // error, but there's no content either.
                return new List<StringTableEntry>();
            }

            var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                Debug.LogError($"Can't generate a strings table from a Yarn Project that contains compile errors", null);
                return null;
            }

            return GetStringTableEntries(compilationResult);
        }

        internal IEnumerable<LineMetadataTableEntry>? GenerateLineMetadataEntries()
        {
            CompilationJob compilationJob = GetCompilationJob();

            if (compilationJob.Inputs.Any() == false)
            {
                // We have no scripts to work with. In this case, return an
                // empty collection - there's no error, but there's no content
                // either.
                return new List<LineMetadataTableEntry>();
            }
            compilationJob.CompilationType = CompilationJob.Type.StringsOnly;

            CompilationResult compilationResult = Compiler.Compiler.Compile(compilationJob);

            var errors = compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                Debug.LogError($"Can't generate line metadata entries from a Yarn Project that contains compile errors", null);
                return null;
            }

            return LineMetadataTableEntriesFromCompilationResult(compilationResult);
        }

        private IEnumerable<StringTableEntry> GetStringTableEntries(CompilationResult result)
        {

            var linesWithContent = result.StringTable.Where(s => s.Value.text != null);

            return linesWithContent.Select(x => new StringTableEntry
            {
                ID = x.Key,
                Language = GetProject()?.BaseLanguage ?? "<unknown language>",
                Text = x.Value.text,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Lock = YarnImporter.GetHashString(x.Value.text!, 8),
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
        private static IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
        {
            return metadata.Where(x => !x.StartsWith("line:"));
        }

#if USE_UNITY_LOCALIZATION
        /// <summary>
        /// Attempts to populate the <see cref="StringTableCollection"/>
        /// associated with this Yarn Project Importer using strings found in
        /// the project's Yarn scripts.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when <see
        /// cref="UseUnityLocalisationSystem"/> is <see
        /// langword="false"/>.</exception>
        internal void AddStringsToUnityLocalization()
        {
            if (UseUnityLocalisationSystem == false)
            {
                throw new System.InvalidOperationException($"Can't add strings to Unity Localization: project {assetPath} does not use Unity Localization.");
            }

            // Get the Yarn string table from the project
            Dictionary<string, StringInfo>? table = GetYarnStringTable();

            if (table == null || ImportData == null)
            {
                // No lines available, or importer has not successfully imported
                return;
            }

            // Get the string table collection from the importer
            StringTableCollection? tableCollection = UnityLocalisationStringTableCollection;

            if (tableCollection == null)
            {
                Debug.LogError("Unable to generate String Table Entries as the string collection is null", (YarnProjectImporter?)this);
                return;
            }

            if (ImportData.baseLanguageName == null)
            {
                Debug.LogError($"Unable to generate String Table Entries as the Yarn Project's {nameof(ImportData.baseLanguageName)} is null", (YarnProjectImporter?)this);
                return;
            }

            // Populate the string table collection from the Yarn strings
            AddStringTableEntries(table, tableCollection, ImportData.baseLanguageName);
        }
#endif

        /// <summary>
        /// A placeholder string that may be used in Yarn Project files that
        /// represents the root path of the Unity project (that is, the
        /// directory containing the Assets folder).
        /// </summary>
        public const string UnityProjectRootVariable = "${UnityProjectRoot}";
    }

    /// <summary>
    /// Contains extension methods for <see cref="Project"/> objects.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Gets the path, relative to the project's location on disk, to the
        /// strings location associated with the given language code.
        /// </summary>
        /// <param name="project">The project to fetch path information
        /// for.</param>
        /// <param name="languageCode">A BCP-47 locale code.</param>
        /// <param name="fullStringsPath">On return, the relative path from
        /// <paramref name="project"/>'s location on disk to the specified
        /// locale's strings location, or <see langword="null"/> if it couldn't be
        /// found.</param>
        /// <returns><see langword="true"/> if the strings location could be found
        /// for the given language code; <see langword="false"/>
        /// otherwise.</returns>
        public static bool TryGetStringsPath(this Yarn.Compiler.Project project, string languageCode, out string? fullStringsPath)
        {
            if (project.Localisation.TryGetValue(languageCode, out var info) == false)
            {
                fullStringsPath = default;
                return false;
            }
            if (string.IsNullOrEmpty(info.Strings))
            {
                fullStringsPath = default;
                return false;
            }

            var projectFolderRelative = Path.GetDirectoryName(project.Path);
            var projectFolderAbsolute = Path.GetFullPath(Path.Combine(YarnProjectImporter.UnityProjectRootPath, projectFolderRelative));

            // am force unwrapping the strings due to a bug
            // the IsNullOrEmpty check on this version of dotnet doesn't propogate it's understanding that the value isn't null
            // in a future version this will go away as a concern.
            var expandedPath = info.Strings!.Replace(YarnProjectImporter.UnityProjectRootVariable, YarnProjectImporter.UnityProjectRootPath);

            if (Path.IsPathRooted(expandedPath) == false)
            {
                expandedPath = Path.GetFullPath(Path.Combine(projectFolderAbsolute, expandedPath));
            }

            fullStringsPath = YarnProjectImporter.GetRelativePath(expandedPath);

            return true;
        }

        /// <summary>
        /// Gets the path, relative to the project's location on disk, to the
        /// assets location associated with the given language code.
        /// </summary>
        /// <param name="project">The project to fetch path information
        /// for.</param>
        /// <param name="languageCode">A BCP-47 locale code.</param>
        /// <param name="fullAssetsPath">On return, the relative path from
        /// <paramref name="project"/>'s location on disk to the specified
        /// locale's assets location, or <see langword="null"/> if it couldn't
        /// be found.</param>
        /// <returns><see langword="true"/> if the assets location could be found
        /// for the given language code; <see langword="false"/>
        /// otherwise.</returns>
        public static bool TryGetAssetsPath(this Yarn.Compiler.Project project, string languageCode, out string? fullAssetsPath)
        {
            if (project.Localisation.TryGetValue(languageCode, out var info) == false)
            {
                fullAssetsPath = default;
                return false;
            }
            if (string.IsNullOrEmpty(info.Assets))
            {
                fullAssetsPath = default;
                return false;
            }
            var projectFolderRelative = Path.GetDirectoryName(project.Path);
            var projectFolderAbsolute = Path.GetFullPath(Path.Combine(YarnProjectImporter.UnityProjectRootPath, projectFolderRelative));

            // am force unwrapping the strings due to a bug
            // the IsNullOrEmpty check on this version of dotnet doesn't propogate it's understanding that the value isn't null
            // in a future version this will go away as a concern.
            var expandedPath = info.Assets!.Replace(YarnProjectImporter.UnityProjectRootVariable, YarnProjectImporter.UnityProjectRootPath);

            if (Path.IsPathRooted(expandedPath) == false)
            {
                expandedPath = Path.GetFullPath(Path.Combine(projectFolderAbsolute, expandedPath));
            }

            fullAssetsPath = YarnProjectImporter.GetRelativePath(expandedPath);

            return true;
        }
    }
}
