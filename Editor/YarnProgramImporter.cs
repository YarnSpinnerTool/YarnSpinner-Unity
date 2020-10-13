using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using System.Linq;
using Yarn.Compiler;
using System.IO;
using UnityEditorInternal;
using System.Collections;

namespace Yarn.Unity
{
    [ScriptedImporter(1, new[] { "yarnprogram" }, 1)]
    public class YarnProgramImporter : ScriptedImporter
    {

        [System.Serializable]
        public class SerializedDeclaration {
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

        public List<TextAsset> sourceScripts = new List<TextAsset>();

        public string compileError;
        public List<SerializedDeclaration> serializedDeclarations = new List<SerializedDeclaration>();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            ctx.LogImportWarning($"Importing {ctx.assetPath}");

            var program = ScriptableObject.CreateInstance<YarnProgram>();

            // Start by creating the asset - no matter what, we need to
            // produce an asset, even if it doesn't contain valid Yarn
            // bytecode, so that other assets don't lose their references.
            ctx.AddObjectToAsset("Program", program);
            ctx.SetMainObject(program);

            foreach (var script in sourceScripts) {
                ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(script));
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
                ctx.LogImportError($"Error in Yarn Program file:{e.Message}");
                compileError = $"Error in Yarn Program {ctx.assetPath}: {e.Message}";
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
            // compile the scripts associated with this program.

            var scriptImporters = sourceScripts.Select(s => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s)) as YarnImporter );

            // First step: check to see if there's any parse errors in the
            // files.
            var scriptsWithParseErrors = scriptImporters.Where(script => script.isSuccesfullyParsed == false);

            if (scriptsWithParseErrors.Count() != 0)
            {
                // Parse errors! We can't continue.
                string failingScriptNameList = string.Join("\n", scriptsWithParseErrors.Select(script => script.assetPath));
                compileError = $"Parse errors exist in the following files:\n{failingScriptNameList}";
                return;
            }

            // Get paths to the scripts we're importing, and also map them to
            // their corresponding importer
            var pathsToImporters = scriptImporters.ToDictionary(script => script.assetPath, script => script);

            if (pathsToImporters.Count == 0)
            {
                ctx.LogImportWarning($"Yarn Program {ctx.assetPath} has no source scripts.");
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

            // Store _all_ declarations - both the ones in this
            // .yarnprogram file, and the ones inside the .yarn files
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

            var unassignedScripts = scriptImporters.Any(s => s.localizationDatabase == null);

            if (unassignedScripts)
            {
                // We have scripts in this program whose lines are not
                // being sent to a localization database. Create a 'default'
                // string table for this program, so that it can be used by
                // a DialogueRunner when it creates its temporary line
                // provider.

                string languageID = scriptImporters.First().baseLanguageID;

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

            if (compilationResult.Program == null)
            {
                ctx.LogImportError("Internal error: Failed to compile: resulting program was null.");
                return;
            }

            byte[] compiledBytes = null;

            ctx.LogImportWarning($"Imported nodes: {string.Join(", ", compilationResult.Program.Nodes.Select(n => n.Key))}");

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

    [CustomEditor(typeof(YarnProgramImporter))]
    public class YarnProgramImporterEditor : ScriptedImporterEditor
    {

        private SerializedProperty compileErrorProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty sourceScriptsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;

        private bool showScripts = true;

        public override void OnEnable()
        {
            base.OnEnable();
            sourceScriptsProperty = serializedObject.FindProperty("sourceScripts");
            compileErrorProperty = serializedObject.FindProperty("compileError");
            serializedDeclarationsProperty = serializedObject.FindProperty("serializedDeclarations");

            serializedDeclarationsList = new ReorderableDeclarationsList(serializedObject, serializedDeclarationsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(compileErrorProperty.stringValue) == false)
            {
                EditorGUILayout.HelpBox(compileErrorProperty.stringValue, MessageType.Error);
            }

            serializedDeclarationsList.DrawLayout();

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No scripts are currently using this Yarn Program.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("This Yarn Program is currently using the following scripts. It will automatically refresh when they change. If you've made a change elsewhere and need to update this Yarn Program, click Update.", MessageType.Info);

                if (GUILayout.Button("Update"))
                {
                    (serializedObject.targetObject as YarnProgramImporter).SaveAndReimport();
                }
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty);


            
            var hadChanges = serializedObject.ApplyModifiedProperties();

#if UNITY_2018
            // Unity 2018's ApplyRevertGUI is buggy, and doesn't automatically
            // detect changes to the importer's serializedObject. This means
            // that we'd need to track the state of the importer, and don't
            // have a way to present a Revert button. 
            //
            // Rather than offer a broken experience, on Unity 2018 we
            // immediately reimport the changes. This is slow (we're
            // serializing and writing the asset to disk on every property
            // change!) but ensures that the writes are done.
            if (hadChanges)
            {
                // Manually perform the same tasks as the 'Apply' button would
                ApplyAndImport();
            }
#endif

#if UNITY_2019_1_OR_NEWER
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that works
            // identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }

        protected override void Apply()
        {
            base.Apply();

            // Get all declarations that came from this program
            var thisProgramDeclarations = new List<Yarn.Compiler.Declaration>();

            for (int i = 0; i < serializedDeclarationsProperty.arraySize; i++) {
                var decl = serializedDeclarationsProperty.GetArrayElementAtIndex(i);
                if (decl.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null) {
                    continue;
                }

                var name = decl.FindPropertyRelative("name").stringValue;

                SerializedProperty typeProperty = decl.FindPropertyRelative("type");

                SerializedProperty defaultValueProperty;

                Type type = (Yarn.Type)typeProperty.enumValueIndex;

                var description = decl.FindPropertyRelative("description").stringValue;

                object defaultValue;
                switch (type)
                {
                    case Yarn.Type.Number:
                        defaultValue = decl.FindPropertyRelative("defaultValueNumber").floatValue;
                        break;
                    case Yarn.Type.String:
                        defaultValue = decl.FindPropertyRelative("defaultValueString").stringValue;
                        break;
                    case Yarn.Type.Bool:
                        defaultValue = decl.FindPropertyRelative("defaultValueBool").boolValue;
                        break;  
                    default:
                        throw new System.ArgumentOutOfRangeException($"Invalid declaration type {type}");
                }

                var declaration = Declaration.CreateVariable(name, defaultValue, description);

                thisProgramDeclarations.Add(declaration);            
            }

            var output = Yarn.Compiler.Utility.GenerateYarnFileWithDeclarations(thisProgramDeclarations, "Program");

            var importer = target as YarnProgramImporter;
            File.WriteAllText(importer.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(importer.assetPath);            
        }
    }

    public class ReorderableDeclarationsList {
        private struct Problem {
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

        public ReorderableDeclarationsList(SerializedObject serializedObject, SerializedProperty property) {
            this.serializedObject = serializedObject;
            serializedProperty = property;
            
            _list = new UnityEditorInternal.ReorderableList(serializedObject, property, false, true, true, true) {
                drawHeaderCallback = (rect) => DrawListHeader(rect, "Declarations"),
                drawElementCallback = (rect, index, isActive, isFocused) => DrawListElement(rect,index,isActive, isFocused, useSearch: true),
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
            var names = declarations.Select((p, index) => new {name=p.FindPropertyRelative("name").stringValue, index});
            
            var duplicateNames = names.GroupBy(s => s.name)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

            problems.AddRange(duplicateNames.Select(name => new Problem {
                    text = $"Duplicate variable name {name}", 
                    index = names.First(n => n.name == name).index 
                }));

            var invalidVariableNames = names.Where(decl => decl.name.Equals(string.Empty) == false && !decl.name.StartsWith("$"));

            problems.AddRange(invalidVariableNames.Select(decl => new Problem { text = $"Variable name '{decl.name}' must begin with a $", index=decl.index}));

            var emptyVariableNames = names.Where(name => string.IsNullOrEmpty(name.name));
            
            problems.AddRange(emptyVariableNames.Select(decl => new Problem { text = $"Variable name must not be empty", index=decl.index}));
            
        }

        IEnumerable<T> Cast<T>(IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T) iterator.Current;
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

        private bool ShouldShowElement(int index) {
            // If we're not searching then all indices are shown
            if (IsSearching == false) {
                return true;
            }

            // Otherwise, we show this element if its index is in the filtered
            // list
            return filteredIndices.Contains(index);
        }

        private float GetElementHeight(int index, bool useSearch)
        {
            if (useSearch == false || ShouldShowElement(index)) {
                var item = serializedProperty.GetArrayElementAtIndex(index);
                return DeclarationPropertyDrawer.GetPropertyHeightImpl(item, null);
            } else {
                return 0;
            }
        }

        private void DrawListHeader(Rect rect, string label) {
            GUI.Label(rect, label);
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused, bool useSearch) {
            if (useSearch == false || ShouldShowElement(index)) {
                var item = serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, item);
            }
        }

        public void DrawLayout() {
            serializedObject.Update();
            EditorGUILayout.Space();

            foreach (var problem in problems) {
                if (problem.index != -1) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                        if (GUILayout.Button("Select", GUILayout.ExpandWidth(false))) {
                            _list.index = problem.index;
                        }                
                    }
                } else {
                    EditorGUILayout.HelpBox(problem.text, MessageType.Error);
                }
                EditorGUILayout.Space();
            }

            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                filterString = EditorGUILayout.TextField("Search", filterString);

                if (changeCheck.changed) {
                    UpdateSearch(filterString);
                }
            }
            
            
            using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                _list.DoLayoutList();

                if (changeCheck.changed) {
                    UpdateProblems();
                }
            }
            

            if (IsSearching && filteredIndices.Count == 0) {
                EditorGUILayout.LabelField("No items to show.");
            }
        }

        private void UpdateSearch(string filterString)
        {
            filteredIndices.Clear();

            if (string.IsNullOrEmpty(filterString) == false) {

                var count = serializedProperty.arraySize;
                for (int i = 0; i < count; i++) {

                    
                    var item = serializedProperty.GetArrayElementAtIndex(i);
                    
                    var nameProperty = item.FindPropertyRelative("name");
                    if (nameProperty.stringValue?.Contains(filterString) ?? false) {
                        filteredIndices.Add(i);
                    }
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(YarnProgramImporter.SerializedDeclaration))]
    public class DeclarationPropertyDrawer: PropertyDrawer {

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
        private void DrawPropertyField(Rect position, SerializedProperty property, bool readOnly, string label = null) {
            if (label == null) {
                label = property.displayName;
            }
            if (readOnly) {
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
                        using (new EditorGUI.DisabledGroupScope(true)) {
                            EditorGUI.ObjectField(position, property);
                        }                        
                        break;
                    case SerializedPropertyType.Enum:
                        var displayValue = property.enumDisplayNames[property.enumValueIndex];
                        EditorGUI.LabelField(position, label, displayValue);
                        break;
                }
            }
            else {
                // Use delayed fields where possible to preserve
                // responsivity (we don't want to force a serialization on
                // Unity 2018 after every keystroke, and delayed fields
                // don't report a change until the user changes focus)
                switch (property.propertyType) {
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            // A serialized declaration is read-only if it came from a Yarn
            // script. We don't allow editing those in this panel, because
            // the text of the Yarn script belongs to the user.
            bool propertyIsReadOnly = property.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null;

            const float leftInset = 8;

            Rect RectForFieldIndex(int index, int lineCount = 1) {
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
            if (string.IsNullOrEmpty(name)) {
                name = "Variable";
            }
            
            property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, name);
            
            if (property.isExpanded) {
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

                
                if (defaultValueProperty == null) {
                    EditorGUI.LabelField(defaultValuePosition, "Default Value", $"Variable type {(Yarn.Type)typeProperty.enumValueIndex} is not allowed");
                } else {
                    DrawPropertyField(defaultValuePosition, defaultValueProperty, propertyIsReadOnly, "Default Value");                    
                }
                
                
                // Don't use DrawPropertyField here because we want to use a special gui style and directly use the string value
                SerializedProperty descriptionProperty = property.FindPropertyRelative("description");
                if (propertyIsReadOnly) {
                    descriptionPosition = EditorGUI.PrefixLabel(descriptionPosition, new GUIContent(descriptionProperty.displayName));
                    EditorGUI.SelectableLabel(descriptionPosition, descriptionProperty.stringValue, EditorStyles.wordWrappedLabel);
                } else {
                    var wordWrappedTextField = EditorStyles.textField;
                    wordWrappedTextField.wordWrap = true;
                
                    descriptionProperty.stringValue = EditorGUI.DelayedTextField(descriptionPosition, descriptionProperty.displayName, descriptionProperty.stringValue, wordWrappedTextField);
                }

                if (!propertyIsReadOnly) {
                    EditorGUI.LabelField(sourcePosition, "Declared In", "this file");
                } else {
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

        public static float GetPropertyHeightImpl(SerializedProperty property, GUIContent label) {
            int lines;

            if (property.isExpanded) {
                lines = 7;                
            } else {
                lines = 1;
            }

            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * lines + 1;
        }


    }
}
