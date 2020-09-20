using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using System.Linq;
using Yarn.Compiler;
using System.IO;

namespace Yarn.Unity
{
    [ScriptedImporter(1, new[] { "yarnprogram" }, 1)]
    public class YarnProgramImporter : ScriptedImporter
    {
        public List<YarnImporter> sourceScripts = new List<YarnImporter>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            ctx.LogImportWarning($"Imported {ctx.assetPath}");
            var program = ScriptableObject.CreateInstance<YarnProgram>();

            ctx.AddObjectToAsset("Program", program);
            ctx.SetMainObject(program);

            // Get the collection of scripts that 1. we're tracking 2.
            // don't think we're tracking
            var extraneousSourceScripts = sourceScripts.Where(script => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(script.destinationProgram)) != this);
            if (extraneousSourceScripts.Count() > 0) {
                sourceScripts = sourceScripts.Except(extraneousSourceScripts).ToList();
                EditorUtility.SetDirty(this);                                
            }

            var inputPaths = sourceScripts.Select(script => script.assetPath)
                                    .ToList();
            
            if (inputPaths.Count == 0) {
                return; // nothing further to do here
            }

            var job = CompilationJob.CreateFromFiles(inputPaths);

            CompilationResult compilationResult;

            try
            {
                compilationResult = Compiler.Compiler.Compile(job);
            }
            catch (TypeException e)
            {
                ctx.LogImportError($"Error compiling: {e.Message}");
                return;
            }
            catch (ParseException e)
            {
                ctx.LogImportError(e.Message);
                return;
            }

            var unassignedScripts = sourceScripts.Any(s => s.localizationDatabase == null);

            if (unassignedScripts) {
                // We have scripts in this program whose lines are not
                // being sent to a localization database. Create a 'default'
                // string table for this program, so that it can be used by
                // a DialogueRunner when it creates its temporary line
                // provider.

                string languageID = sourceScripts.First().baseLanguageID;                    

                var lines = compilationResult.StringTable
                    .Select(x =>
                        {
                            return new StringTableEntry
                            {
                                ID = x.Key,
                                Language = languageID,
                                Text = x.Value.text,
                                File = x.Value.fileName,
                                Node = x.Value.nodeName,
                                LineNumber = x.Value.lineNumber.ToString(),
                                Lock = YarnImporter.GetHashString(x.Value.text, 8),
                            };
                        })
                    .OrderBy(entry => entry.File)
                    .ThenBy(entry => int.Parse(entry.LineNumber));

                var defaultStringTableCSV = StringTableEntry.CreateCSV(lines);
                var defaultStringTable = new TextAsset(defaultStringTableCSV)
                {
                    name = $"{Path.GetFileNameWithoutExtension(ctx.assetPath)} Default String Table ({languageID})"
                };

                // Hide this asset - it's not editable and can't be
                // exported for localization (it only exists because a
                // script isn't using the localization system!). As a
                // result, we'll save it to disk, but not expose it as a
                // file.
                defaultStringTable.hideFlags = HideFlags.HideInHierarchy;

                ctx.AddObjectToAsset("Strings", defaultStringTable);
                
                program.defaultStringTable = defaultStringTable;
            }            

            if (compilationResult.Program != null)
            {
                byte[] compiledBytes = null;

                using (var memoryStream = new MemoryStream())
                using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
                {
                    // Serialize the compiled program to memory
                    compilationResult.Program.WriteTo(outputStream);
                    outputStream.Flush();

                    compiledBytes = memoryStream.ToArray();
                }

                program.compiledYarnProgram = compiledBytes;
            }
        }
    }

    [CustomEditor(typeof(YarnProgramImporter))]
    public class YarnProgramImporterEditor : Editor {

        private SerializedProperty sourceScriptsProperty;

        private bool showScripts = true;

        private void OnEnable() {
            sourceScriptsProperty = serializedObject.FindProperty("sourceScripts");
        }

        public override void OnInspectorGUI() {
            //base.OnInspectorGUI();

            EditorGUILayout.Space();

            showScripts = EditorGUILayout.Foldout(showScripts, "Source Scripts");

            if (showScripts)
            {

                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUI.indentLevel += 1;
                    foreach (SerializedProperty sourceScriptProperty in sourceScriptsProperty)
                    {
                        YarnImporter yarnImporter = (sourceScriptProperty.objectReferenceValue as YarnImporter);
                        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(yarnImporter.assetPath);
                        EditorGUILayout.ObjectField(asset, typeof(TextAsset), false);
                    }
                    EditorGUI.indentLevel -= 1;

                }
            }
            
            
        }
    }
}
