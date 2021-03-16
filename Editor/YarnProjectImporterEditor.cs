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

namespace Yarn.Unity
{
    [CustomEditor(typeof(YarnProjectImporter))]
    public class YarnProjectImporterEditor : ScriptedImporterEditor
    {
        // A runtime-only field that stores the defaultLanguage of the
        // YarnProjectImporter. Used during Inspector GUI drawing.
        internal static SerializedProperty CurrentProjectDefaultLanguageProperty;

        private SerializedProperty compileErrorProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty defaultLanguageProperty;
        private SerializedProperty sourceScriptsProperty;
        private SerializedProperty languagesToSourceAssetsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;

        public override void OnEnable()
        {
            base.OnEnable();
            sourceScriptsProperty = serializedObject.FindProperty("sourceScripts");
            compileErrorProperty = serializedObject.FindProperty("compileError");
            serializedDeclarationsProperty = serializedObject.FindProperty("serializedDeclarations");

            defaultLanguageProperty = serializedObject.FindProperty("defaultLanguage");
            languagesToSourceAssetsProperty = serializedObject.FindProperty("languagesToSourceAssets");

            serializedDeclarationsList = new ReorderableDeclarationsList(serializedObject, serializedDeclarationsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This Yarn Project has no content. Add Yarn Scripts to it.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty, true);

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(compileErrorProperty.stringValue) == false)
            {
                EditorGUILayout.HelpBox(compileErrorProperty.stringValue, MessageType.Error);
            }

            serializedDeclarationsList.DrawLayout();

            EditorGUILayout.PropertyField(defaultLanguageProperty, new GUIContent("Default Language"));

            CurrentProjectDefaultLanguageProperty = defaultLanguageProperty;

            EditorGUILayout.PropertyField(languagesToSourceAssetsProperty);

            CurrentProjectDefaultLanguageProperty = null;

            YarnProjectImporter yarnProjectImporter = serializedObject.targetObject as YarnProjectImporter;

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
#endif
                canGenerateStringsTable = false;
            }

            using (new EditorGUI.DisabledScope(canGenerateStringsTable == false))
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

            using (new EditorGUI.DisabledScope(canGenerateStringsTable == true || hasAnyTextAssets == false))
            {
                if (GUILayout.Button("Add Line Tags to Scripts"))
                {
                    YarnProjectUtility.AddLineTagsToFilesInYarnProject(yarnProjectImporter);
                }
            }

            var hadChanges = serializedObject.ApplyModifiedProperties();

            if (hadChanges)
            {
                Debug.Log($"{nameof(YarnProjectImporterEditor)} had changes");
            }

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

                SerializedProperty typeProperty = decl.FindPropertyRelative("type");

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
        /// field <see cref="SiblingFieldName"/> has a string value equal to
        /// the <see cref="SerializedProperty"/> field in <see
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

            if (targetProperty == null) {
                // We couldn't find it. Log a warning, draw the original
                // UI, and return.
                Debug.LogWarning($"Property not found at path {targetPropertyPath}");
                defaultDraw.Invoke(null, new object[3] { position, property, label });
            }

            // Ensure that they're both strings.
            if (targetProperty.propertyType != SerializedPropertyType.String || contextProperty.propertyType != SerializedPropertyType.String) {
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
