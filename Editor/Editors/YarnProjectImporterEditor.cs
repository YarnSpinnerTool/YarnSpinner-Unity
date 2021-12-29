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
using System.Reflection;

#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(YarnProjectImporter))]
    public class YarnProjectImporterEditor : ScriptedImporterEditor
    {
        // A runtime-only field that stores the defaultLanguage of the
        // YarnProjectImporter. Used during Inspector GUI drawing.
        internal static SerializedProperty CurrentProjectDefaultLanguageProperty;

        private SerializedProperty compileErrorsProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty defaultLanguageProperty;
        private SerializedProperty sourceScriptsProperty;
        private SerializedProperty languagesToSourceAssetsProperty;
        private SerializedProperty useAddressableAssetsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;
        private SerializedProperty searchAllAssembliesProperty;
        private SerializedProperty assembliesToSearchProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            sourceScriptsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.sourceScripts));
            compileErrorsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.compileErrors));
            serializedDeclarationsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.serializedDeclarations));

            defaultLanguageProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.defaultLanguage));
            languagesToSourceAssetsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.languagesToSourceAssets));

            useAddressableAssetsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.useAddressableAssets));

            serializedDeclarationsList = new ReorderableDeclarationsList(serializedObject, serializedDeclarationsProperty);

            searchAllAssembliesProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.searchAllAssembliesForActions));
            assembliesToSearchProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.assembliesToSearch));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            YarnProjectImporter yarnProjectImporter = serializedObject.targetObject as YarnProjectImporter;

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This Yarn Project has no content. Add Yarn Scripts to it.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty, true);

            EditorGUILayout.Space();

            bool hasCompileError = compileErrorsProperty.arraySize > 0;

            if (hasCompileError)
            {
                foreach (SerializedProperty compileError in compileErrorsProperty) {
                    EditorGUILayout.HelpBox(compileError.stringValue, MessageType.Error);
                }
            }

            serializedDeclarationsList.DrawLayout();

            // The 'Convert Implicit Declarations' feature has been
            // temporarily removed in v2.0.0-beta5.

#if false
            // If any of the serialized declarations are implicit, add a
            // button that lets you generate explicit declarations for them
            var anyImplicitDeclarations = false;
            foreach (SerializedProperty declProp in serializedDeclarationsProperty) {
                anyImplicitDeclarations |= declProp.FindPropertyRelative("isImplicit").boolValue;
            }
            
            if (hasCompileError == false && anyImplicitDeclarations) {
                if (GUILayout.Button("Convert Implicit Declarations")) {
                    // add explicit variable declarations to the file
                    YarnProjectUtility.ConvertImplicitVariableDeclarationsToExplicit(yarnProjectImporter);

                    // Return here becuase this method call will cause the
                    // YarnProgram contents to change, which confuses the
                    // SerializedObject when we're in the middle of a GUI
                    // draw call. So, stop here, and let Unity re-draw the
                    // Inspector (which it will do on the next editor tick
                    // because the item we're inspecting got re-imported.)
                    return;
                }
            }
#endif

            EditorGUILayout.PropertyField(defaultLanguageProperty, new GUIContent("Base Language"));

            CurrentProjectDefaultLanguageProperty = defaultLanguageProperty;

            EditorGUILayout.PropertyField(languagesToSourceAssetsProperty, new GUIContent("Localisations"));

            CurrentProjectDefaultLanguageProperty = null;

            // Ask the project importer if it can generate a strings table.
            // This involves querying several assets, which means various
            // exceptions might get thrown, which we'll catch and log (if
            // we're in debug mode).
            bool canGenerateStringsTable;

            try
            {
                canGenerateStringsTable = yarnProjectImporter.CanGenerateStringsTable;
            }
            catch (System.Exception e)
            {
#if YARNSPINNER_DEBUG
                Debug.LogWarning($"Encountered in error when checking to see if Yarn Project Importer could generate a strings table: {e}", this);
#else
                // Ignore the 'variable e is unused' warning
                var _ = e;
#endif
                canGenerateStringsTable = false;
            }

            // The following controls only do something useful if all of
            // the lines in the project have tags, which means the project
            // can generate a string table.
            using (new EditorGUI.DisabledScope(canGenerateStringsTable == false))
            {
#if USE_ADDRESSABLES
                
                // If the addressable assets package is available, show a
                // checkbox for using it.
                var hasAnySourceAssetFolders = yarnProjectImporter.languagesToSourceAssets.Any(l => l.assetsFolder != null);
                if (hasAnySourceAssetFolders == false) {
                    // Disable this checkbox if there are no assets
                    // available.
                    using (new EditorGUI.DisabledScope(true)) {
                        EditorGUILayout.Toggle(useAddressableAssetsProperty.displayName, false);
                    }
                } else {
                    EditorGUILayout.PropertyField(useAddressableAssetsProperty);

                    // Show a warning if we've requested addressables but
                    // haven't set it up.
                    if (useAddressableAssetsProperty.boolValue && AddressableAssetSettingsDefaultObject.SettingsExists == false) {
                        EditorGUILayout.HelpBox("Please set up Addressable Assets in this project.", MessageType.Warning);
                    }
                }

                // Add a button for updating asset addresses, if any asset
                // source folders exist
                if (useAddressableAssetsProperty.boolValue && AddressableAssetSettingsDefaultObject.SettingsExists) {
                    using (new EditorGUI.DisabledScope(hasAnySourceAssetFolders == false)) {
                        if (GUILayout.Button($"Update Asset Addresses")) {
                            YarnProjectUtility.UpdateAssetAddresses(yarnProjectImporter);
                        }
                    }
                }
#endif
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Commands and Functions", EditorStyles.boldLabel);

            var searchAllAssembliesLabel = new GUIContent("Search All Assemblies", "Search all assembly definitions for commands and functions, as well as code that's not in a folder with an assembly definition");
            EditorGUILayout.PropertyField(searchAllAssembliesProperty, searchAllAssembliesLabel);

            if (searchAllAssembliesProperty.boolValue == false)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(assembliesToSearchProperty);
                EditorGUI.indentLevel -= 1;
            }

            using (new EditorGUI.DisabledGroupScope(canGenerateStringsTable == false))
            {
                if (GUILayout.Button("Export Strings as CSV"))
                {
                    var currentPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                    var currentFileName = Path.GetFileNameWithoutExtension(currentPath);
                    var currentDirectory = Path.GetDirectoryName(currentPath);

                    var destinationPath = EditorUtility.SaveFilePanel("Export Strings CSV", currentDirectory, $"{currentFileName}.csv", "csv");

                    if (string.IsNullOrEmpty(destinationPath) == false)
                    {
                        // Generate the file on disk
                        YarnProjectUtility.WriteStringsFile(destinationPath, yarnProjectImporter);

                        // destinationPath may have been inside our Assets
                        // directory, so refresh the asset database
                        AssetDatabase.Refresh();
                    }
                }
                if (yarnProjectImporter.languagesToSourceAssets.Count > 0)
                {
                    if (GUILayout.Button("Update Existing Strings Files"))
                    {
                        YarnProjectUtility.UpdateLocalizationCSVs(yarnProjectImporter);
                    }
                }
            }

            // Does this project's source scripts list contain any actual
            // assets? (It can have a count of >0 and still have no assets
            // when, for example, you've just clicked the + button but
            // haven't dragged an asset in yet.)
            var hasAnyTextAssets = yarnProjectImporter.sourceScripts.Where(s => s != null).Count() > 0;

            // Disable this button if 1. all lines already have tags or 2.
            // no actual source files exist
            using (new EditorGUI.DisabledScope(canGenerateStringsTable == true || hasAnyTextAssets == false))
            {
                if (GUILayout.Button("Add Line Tags to Scripts"))
                {
                    YarnProjectUtility.AddLineTagsToFilesInYarnProject(yarnProjectImporter);
                }
            }

            var hadChanges = serializedObject.ApplyModifiedProperties();

#if UNITY_2018
            // Unity 2018's ApplyRevertGUI is buggy, and doesn't
            // automatically detect changes to the importer's
            // serializedObject. This means that we'd need to track the
            // state of the importer, and don't have a way to present a
            // Revert button. 
            //
            // Rather than offer a broken experience, on Unity 2018 we
            // immediately reimport the changes. This is slow (we're
            // serializing and writing the asset to disk on every property
            // change!) but ensures that the writes are done.
            if (hadChanges)
            {
                // Manually perform the same tasks as the 'Apply' button
                // would
                ApplyAndImport();
            }
#endif

#if UNITY_2019_1_OR_NEWER
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that
            // works identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }



        protected override void Apply()
        {
            base.Apply();

            // Get all declarations that came from this program
            var thisProgramDeclarations = new List<Yarn.Compiler.Declaration>();

            for (int i = 0; i < serializedDeclarationsProperty.arraySize; i++)
            {
                var decl = serializedDeclarationsProperty.GetArrayElementAtIndex(i);
                if (decl.FindPropertyRelative("sourceYarnAsset").objectReferenceValue != null)
                {
                    continue;
                }

                var name = decl.FindPropertyRelative("name").stringValue;

                SerializedProperty typeProperty = decl.FindPropertyRelative("typeName");

                Yarn.IType type = YarnProjectImporter.SerializedDeclaration.BuiltInTypesList.FirstOrDefault(t => t.Name == typeProperty.stringValue);

                var description = decl.FindPropertyRelative("description").stringValue;

                System.IConvertible defaultValue;

                if (type == Yarn.BuiltinTypes.Number) {
                    defaultValue = decl.FindPropertyRelative("defaultValueNumber").floatValue;
                } else if (type == Yarn.BuiltinTypes.String) {
                    defaultValue = decl.FindPropertyRelative("defaultValueString").stringValue;
                } else if (type == Yarn.BuiltinTypes.Boolean) {
                    defaultValue = decl.FindPropertyRelative("defaultValueBool").boolValue;
                } else {
                    throw new System.ArgumentOutOfRangeException($"Invalid declaration type {type.Name}");
                }
                
                var declaration = Declaration.CreateVariable(name, type, defaultValue, description);

                thisProgramDeclarations.Add(declaration);
            }

            var output = Yarn.Compiler.Utility.GenerateYarnFileWithDeclarations(thisProgramDeclarations, "Program");

            var importer = target as YarnProjectImporter;
            File.WriteAllText(importer.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(importer.assetPath);
        }
    }

    /// <summary>
    /// An attribute that causes this property to be drawn as a read-only
    /// label if a second property is equal to a specific static field on a
    /// specific class.
    /// </summary>
    /// <remarks>
    /// This attribute is used in order to make the 'stringsFile' field on
    /// YarnProjectImporter.LanguageToSourceAsset appear as a read-only
    /// label when its 'languageID' is equal to the default language of the
    /// YarnProjectImporter currently being inspected.
    ///
    /// Yes, this is a really convoluted approach, but this is the best I
    /// could come up with considering I didn't want to re-implement
    /// drawing the entire list, and I can't pass contextual information to
    /// a property.
    /// </remarks>
    internal class HideWhenPropertyValueEqualsContextAttribute : PropertyAttribute
    {
        /// <summary>
        /// The class that contains a static field with the name given in
        /// <see cref="FieldNameInOtherClass"/>.
        /// </summary>
        public System.Type OtherClassType;

        /// <summary>
        /// The name of the static field found in <see
        /// cref="OtherClassType"/>. This field must be of type <see
        /// cref="SerializedProperty"/>.
        /// </summary>
        /// <remarks>
        /// You are strongly encouraged to use <code>nameof</code> to refer
        /// to this member.
        /// </remarks>
        public string FieldNameInOtherClass;

        /// <summary>
        /// The name of the field whose value should be checked against.
        /// This field must be a sibling of the field this attribute is
        /// applied to.
        /// </summary>
        public string SiblingFieldName;

        /// <summary>
        /// The label to show for this field in the Inspector when the
        /// field <see cref="SiblingFieldName"/> has a string value equal
        /// to the <see cref="SerializedProperty"/> field in <see
        /// cref="OtherClassType"/>.
        /// </summary>
        public string DisplayStringWhenEmpty;

        public HideWhenPropertyValueEqualsContextAttribute(string siblingFieldName, System.Type classType, string fieldNameInOtherClass, string displayStringWhenEmpty)
        {
            OtherClassType = classType;
            FieldNameInOtherClass = fieldNameInOtherClass;
            SiblingFieldName = siblingFieldName;
            DisplayStringWhenEmpty = displayStringWhenEmpty;
        }
    }

    /// <summary>
    /// The custom editor for drawing properties that have the
    /// HideWhenPropertyValueEqualsContextAttribute applied to them.
    /// </summary>
    [CustomPropertyDrawer(typeof(HideWhenPropertyValueEqualsContextAttribute))]
    internal class HideWhenValueEqualsContextAttributeEditor : PropertyDrawer
    {
        /// <summary>
        /// Called by Unity to draw the GUI for properties that this editor
        /// applies to.
        /// </summary>
        /// <param name="position">The rectangle to draw content
        /// in.</param>
        /// <param name="property">The property to draw.</param>
        /// <param name="label">The label to draw for this
        /// property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the HideWhenPropertyValueEqualsContextAttribute that
            // caused this drawer to be invoked.
            var attribute = this.attribute as HideWhenPropertyValueEqualsContextAttribute;

            // Get the data out of the attribute.
            System.Type classType = attribute.OtherClassType;
            string fieldName = attribute.FieldNameInOtherClass;
            string displayStringWhenEmpty = attribute.DisplayStringWhenEmpty;

            // Get the static field that the attribute wants to access.
            System.Reflection.FieldInfo field = classType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Get the internal method EditorGUI.DefaultPropertyField,
            // which we'll call when we need to draw the original property
            // UI.
            MethodInfo defaultDraw = typeof(EditorGUI)
                .GetMethod("DefaultPropertyField", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            // Is the static field a serialized property?
            if (field.FieldType.IsAssignableFrom(typeof(SerializedProperty)) == false)
            {
                // The field we were aimed at is not a serialized property.
                // Early out.
                defaultDraw.Invoke(null, new object[3] { position, property, label });
                return;
            }

            // Get the context property from the class's static field.
            SerializedProperty contextProperty = field.GetValue(null) as SerializedProperty;

            // Next, get the property we're comparing to. This is required
            // to be a sibling property of the one that had this attribute.

            // We'll do this by taking the path to this property, removing
            // the last element, and appending the target property name.
            // We'll then use FindProperty to locate it.
            var targetPropertyPath = string.Join(".", property.propertyPath.Split('.').Reverse().Skip(1).Reverse().Append(attribute.SiblingFieldName));

            var targetProperty = property.serializedObject.FindProperty(targetPropertyPath);

            if (targetProperty == null)
            {
                // We couldn't find it. Log a warning, draw the original
                // UI, and return.
                Debug.LogWarning($"Property not found at path {targetPropertyPath}");
                defaultDraw.Invoke(null, new object[3] { position, property, label });
            }

            // Ensure that they're both strings.
            if (targetProperty.propertyType != SerializedPropertyType.String || contextProperty.propertyType != SerializedPropertyType.String)
            {
                // They're not both strings. Draw as usual.
                //
                // (This restriction exists because, weirdly,
                // SerializedProperty.DataEquals() on two properties that
                // contained the strings "en-AU" and "en-US" was returning
                // true. Restricting it to strings and doing a string
                // comparison fixed this.)
                defaultDraw.Invoke(null, new object[3] { position, property, label });
            }

            // Finally, the moment of truth: compare the two strings.
            if (contextProperty.stringValue.Equals(targetProperty.stringValue, System.StringComparison.InvariantCulture))
            {
                // The data matches. We don't want to show this property.
                // Show the label instead.
                EditorGUI.LabelField(position, label, new GUIContent(displayStringWhenEmpty));
            }
            else
            {
                // The data doesn't match. Draw as usual.
                defaultDraw.Invoke(null, new object[3] { position, property, label });
            }
        }
    }

}
